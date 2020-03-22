using System;
using System.Collections.Generic;
using MiniPL.Common;
using MiniPL.Common.AST;
using MiniPL.Common.Errors;
using MiniPL.Common.Symbols;
using static MiniPL.Common.Util;

namespace MiniPL.Interpret
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
                    var type = SymbolTable.LookupSymbol(id);
                    var input = Console.ReadLine();
                    var inputValues = input.Split(new[] {'\n', ' ', '\t', '\r'});

                    if (inputValues.Length == 0 || 
                        inputValues[0].Equals("") || 
                        inputValues.Length > 1)
                    {
                        ErrorService.Add(
                            ErrorType.InputError,
                            node.Arguments[0].Token,
                            $"invalid input: {input}",
                            true
                        );
                    }

                    var value = inputValues[0];
                    var guessedType = GuessType(value);

                    var errorType = _memory.UpdateVariable(id, value);

                    if (errorType == ErrorType.TypeError || guessedType != type)
                    {
                        ErrorService.Add(
                            ErrorType.TypeError,
                            node.Arguments[0].Token,
                            $"type error: expected {SymbolTable.LookupSymbol(id)}, got {guessedType}",
                            true);
                        // should handle control variable error by itself 
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


        public override object Visit(ExpressionNode node)
        {
            return node.Expression.Accept(this);
        }

        public override object Visit(BinaryNode node)
        {
            var operand1 = node.Left.Accept(this);
            var operand2 = node.Right.Accept(this);
            var op = node.Operator; //Token.OperatorToOperatorType.TryGetValueOrDefault(node.Token.Content);

            switch (op)
            {
                case OperatorType.Addition when operand1 is int o1 && operand2 is int o2:
                    return o1 + o2;
                case OperatorType.Addition when operand1 is string o1 && operand2 is string o2:
                    return o1 + o2;
                case OperatorType.Subtraction when operand1 is int o1 && operand2 is int o2:
                    return o1 - o2;
                case OperatorType.Multiplication when operand1 is int o1 && operand2 is int o2:
                    return o1 * o2;
                case OperatorType.Division when operand1 is int o1 && operand2 is int o2:
                    return o1 / o2;
                case OperatorType.And when operand1 is bool o1 && operand2 is bool o2:
                    return o1 && o2;
                case OperatorType.LessThan when operand1 is int o1 && operand2 is int o2:
                    return o1 < o2;
                case OperatorType.LessThan when operand1 is string o1 && operand2 is string o2:
                    return string.CompareOrdinal(o1, o2) < 0;
                case OperatorType.LessThan when operand1 is bool o1 && operand2 is bool o2:
                    return !o1 && o2;
                case OperatorType.Equals:
                    return operand1.Equals(operand2);
                default:
                    // this should have been already caught in semantic analysis
                    ErrorService.Add(
                        ErrorType.InvalidOperation,
                        node.Token,
                        $"invalid binary operation {node.Token.Content}"
                    );
                    break;
            }

            return null;
        }

        public override object Visit(UnaryNode node)
        {
            try
            {
                return node.Operator switch
                {
                    OperatorType.Subtraction => (object) -(int) node.Value.Accept(this),
                    OperatorType.Not => !(bool) node.Value.Accept(this),
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