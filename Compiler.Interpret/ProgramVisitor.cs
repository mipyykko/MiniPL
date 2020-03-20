using System;
using System.Collections.Generic;
using Compiler.Common;
using Compiler.Common.AST;
using Compiler.Common.Errors;
using static Compiler.Common.Util;

namespace Compiler.Interpret
{
    public class ProgramVisitor : Visitor
    {
        private IErrorService ErrorService => Context.ErrorService;
        private ISymbolTable SymbolTable => Context.SymbolTable;
        private IProgramMemory _memory;
        
        public ProgramVisitor(IProgramMemory memory)
        {
            _memory = memory;
        }
        
        public override object Visit(StatementNode node)
        {
            switch (node.Token.KeywordType)
            {
                case KeywordType.Print:
                    Console.Write(node.Arguments[0].Accept(this));
                    break;
                case KeywordType.Assert:
                    var result = (bool) node.Arguments[0].Accept(this);
                    if (result != true)
                    {
                        ErrorService.Add(
                            ErrorType.AssertionError, 
                            node.Token, 
                            $"assertion failed: {node.Arguments[0].Representation()}",
                        true
                        );
                    }

                    break;
                case KeywordType.Read:
                {
                    var id = ((VariableNode) node.Arguments[0]).Token.Content;
                    var value = Console.ReadLine();

                    var errorType = _memory.UpdateVariable(id, value);

                    switch (errorType)
                    {
                        case ErrorType.TypeError:
                            ErrorService.Add(
                                errorType, 
                                node.Arguments[0].Token, 
                                $"type error: expected {SymbolTable.LookupSymbol(id)}, got {GuessType(value)}", 
                                true);
                            break;
                        default:
                            break;
                    }

                    break;
                }
            }

            return null;
        }

        public override object Visit(ForNode node)
        {
            var id = node.Id.Token.Content;
            var rangeStart = (int) node.RangeStart.Accept(this);
            var rangeEnd = (int) node.RangeEnd.Accept(this);

            var direction = rangeStart <= rangeEnd ? 1 : -1;

            SymbolTable.SetControlVariable(id);

            _memory.UpdateControlVariable(id, rangeStart);

            bool Condition(int i) => direction > 0 ? i <= rangeEnd : i >= rangeEnd;

            var i = rangeStart;

            while (Condition(i))
            {
                node.Statements.Accept(this);
                i += direction;
                _memory.UpdateControlVariable(id, i);
            }

            SymbolTable.UnsetControlVariable(id);

            return null;
        }

        public override object Visit(NoOpNode node)
        {
            return null;
        }

        public override object Visit(StatementListNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            return null;
        }

        public Dictionary<string, OperatorType> OperatorToOperatorType = new Dictionary<string, OperatorType>()
        {
            ["*"] = OperatorType.Multiplication,
            ["/"] = OperatorType.Division,
            ["+"] = OperatorType.Addition,
            ["-"] = OperatorType.Subtraction,
            ["&"] = OperatorType.And,
            ["="] = OperatorType.Equals,
            ["<"] = OperatorType.LessThan,
            ["!"] = OperatorType.Not
        };

        public override object Visit(ExpressionNode node)
        {
            return node.Expression.Accept(this);
        }

        public override object Visit(BinaryNode node)
        {
            var opnd1 = node.Left.Accept(this);
            var opnd2 = node.Right.Accept(this);
            var op = OperatorToOperatorType.TryGetValueOrDefault(node.Token.Content);

            switch (op)
            {
                case OperatorType.Addition when opnd1 is int o1 && opnd2 is int o2:
                    return o1 + o2;
                case OperatorType.Addition when opnd1 is string o1 && opnd2 is string o2:
                    return o1 + o2;
                case OperatorType.Subtraction when opnd1 is int o1 && opnd2 is int o2:
                    return o1 - o2;
                case OperatorType.Multiplication when opnd1 is int o1 && opnd2 is int o2:
                    return o1 * o2;
                case OperatorType.Division when opnd1 is int o1 && opnd2 is int o2:
                    return o1 / o2;
                case OperatorType.And when opnd1 is bool o1 && opnd2 is bool o2:
                    return o1 && o2;
                case OperatorType.LessThan when opnd1 is int o1 && opnd2 is int o2:
                    return o1 < o2;
                case OperatorType.LessThan when opnd1 is string o1 && opnd2 is string o2:
                    return string.CompareOrdinal(o1, o2) < 0;
                case OperatorType.LessThan when opnd1 is bool o1 && opnd2 is bool o2:
                    return !o1 && o2;
                case OperatorType.Equals:
                    return opnd1.Equals(opnd2);
                default:
                    ErrorService.Add(
                        ErrorType.InvalidOperation, 
                        node.Token, 
                    $"invalid binary operation {node.Token.Content}" // TODO: operands?
                    );
                    break;
            }

            return null;
        }

        public override object Visit(UnaryNode node)
        {
            try
            {
                return node.Token.Content switch
                {
                    "-" => (object) -(int) node.Value.Accept(this),
                    "!" => !(bool) node.Value.Accept(this),
                    _ => throw new InvalidOperationException()
                };
            }
            catch (Exception)
            {
                ErrorService.Add(
                    ErrorType.InvalidOperation,
                    node.Token,
                    $"invalid unary operator {node.Token.Content}"
                );
                return null;
            }
        }

        public override object Visit(AssignmentNode node)
        {
            var id = node.Id.Token.Content;
            var value = node.Expression.Accept(this);
            var type = node.Type;

            var newValue = value ?? DefaultValue(type);
            
            _memory.UpdateVariable(id, newValue);

            return newValue;
        }

        public override object Visit(LiteralNode node)
        {
            return _memory.ParseResult(node.Type, node.Token.Content); // TODO: parsing in wrong place
        }

        public override object Visit(VariableNode node)
        {
            var id = node.Token.Content;
            if (!SymbolTable.SymbolExists(id))
            {
                ErrorService.Add(ErrorType.UndeclaredVariable, node.Token, $"{id} not declared");
            }

            return _memory.LookupVariable(id);
        }
    }
}