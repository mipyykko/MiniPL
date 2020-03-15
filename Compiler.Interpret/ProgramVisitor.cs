using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Compiler.Common;
using Compiler.Common.AST;
using Compiler.Symbols;
using Node = Compiler.Common.AST.Node;

namespace Compiler.Interpret
{
    public class ProgramVisitor : Visitor
    {
        private Text _source;
        private SymbolTable _symbolTable;

        public ProgramVisitor(SymbolTable symbolTable, Text source)
        {
            _symbolTable = symbolTable;
            _source = source;
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
                        throw new Exception("assertion failed");
                    }

                    break;
                case KeywordType.Read:
                {
                    var id = ((VariableNode) node.Arguments[0]).Token.Content;
                    var type = (PrimitiveType) _symbolTable.LookupType(id);
                    var value = Console.ReadLine();

                    _symbolTable.ParseResult(type, value); // does it error?
                    _symbolTable.UpdateSymbol(id, value);
                    break;
                }
                default:
                    break;
            }

            return null;
        }

        private (PrimitiveType, object) FindType(object value) 
        {
            if (value is ValueTuple<PrimitiveType, object>)
            {
                return (ValueTuple<PrimitiveType, object>) value;
            }

            if (value is int)
            {
                return (PrimitiveType.Int, value);
            }
            return (Util.GuessType((string) value), value);
        }
        
        public override object Visit(ForNode node)
        {
            var id = node.Token.Content;
            var (rangeStartType, rangeStart) = FindType(node.RangeStart.Accept(this));
            var (rangeEndType, rangeEnd) = FindType(node.RangeEnd.Accept(this));
            var error = ErrorType.Unknown;
            
            //if (!(rangeStart is int) || !(rangeEnd is int))
            if (rangeStartType != PrimitiveType.Int || rangeEndType != PrimitiveType.Int)
            {
                ThrowError(ErrorType.InvalidRange,
                    node,
                    $"invalid range {rangeStart}..{rangeEnd}");
            }

            if (!_symbolTable.SymbolExists(id)) // TODO
            {
                ThrowError(ErrorType.UndeclaredVariable,
                    node,
                    $"control variable {id} not declared");
            }

            error = _symbolTable.UpdateSymbol(id, (int) rangeStart);
            if (error != ErrorType.Unknown)
            {
                ThrowError(error,
                    node,
                    $"unable to assign to control variable {id}");
            }

            _symbolTable.SetControlVariable(id);
            // ControlVariables[id] = true;
            var i = (int) rangeStart;

            while (i <= (int) rangeEnd)
            {
                node.Statements.Accept(this);
                i++;
                error = _symbolTable.UpdateControlVariable(id, i);
                if (error != ErrorType.Unknown)
                {
                    ThrowError(error,
                        node,
                        $"unable to assign to control variable {id}");
                }
            }

            _symbolTable.UnsetControlVariable(id);

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

        public Dictionary<string, OperatorType> ToOperatorType = new Dictionary<string, OperatorType>()
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
            var (opnd1Type, opnd1) = FindType(node.Left.Accept(this));
            var (opnd2Type, opnd2) = FindType(node.Right.Accept(this));
            var op = ToOperatorType.TryGetValueOrDefault(node.Token.Content);

            switch (op)
            {
                case OperatorType.Addition when opnd1Type == PrimitiveType.Int && opnd2Type == PrimitiveType.Int: // opnd1 is int o1 && opnd2 is int o2:
                    return (int) opnd1 + (int) opnd2;//o1 + o2;
                case OperatorType.Addition when opnd1Type == PrimitiveType.String && opnd2Type == PrimitiveType.String: // opnd1 is string o1 && opnd2 is string o2:
                    return (string) opnd1 + (string) opnd2;//o1 + o2;
                case OperatorType.Addition:
                    ThrowError(ErrorType.InvalidOperation,
                        node,
                        $"invalid operation ${op}");
                    break;
                case OperatorType.Subtraction:
                    return (int) opnd1 - (int) opnd2;
                case OperatorType.Multiplication:
                    return (int) opnd1 * (int) opnd2;
                case OperatorType.Division:
                    return (int) opnd1 / (int) opnd2;
                case OperatorType.And:
                    return (bool) opnd1 && (bool) opnd2;
                case OperatorType.LessThan:
                    return (int) opnd1 < (int) opnd2;
                case OperatorType.Equals:
                    return opnd1.Equals(opnd2);
            }

            return null;
        }

        public override object Visit(UnaryNode node)
        {
            return node.Token.Content switch
            {
                "-" => (object) -(int) node.Value.Accept(this),
                "!" => !(bool) node.Value.Accept(this),
                _ => ThrowError(ErrorType.InvalidOperation,
                        node,
                        $"invalid unary operator {node.Token.Content}")
            };
        }

        public override object Visit(AssignmentNode node)
        {
            var id = node.Id.Token.Content;
            var value = node.Expression.Accept(this);
            var type = node.Type;

            if (node.Token?.KeywordType != KeywordType.Var)
            {
                if (!_symbolTable.SymbolExists(id))
                {
                    ThrowError(ErrorType.UndeclaredVariable,
                        node,
                        $"variable {id} not declared");
                }
            }
            

            _symbolTable.UpdateSymbol(id, value);
            Debug.WriteLine($"modified {id} {type} {value}");

            return value;
        }

        private object ThrowError(ErrorType type, Node node, string message)
        {
            var errorLine = node.Token.SourceInfo.LineRange.Line;
            Console.WriteLine($"\nError: {message} on line {errorLine}:");
            for (var i = Math.Max(0, errorLine - 2); i < Math.Min(_source.Lines.Count, errorLine + 3); i++)
            {
                Console.WriteLine($"{i}: {_source.Lines[i]}");
                
            }
            Environment.Exit(1);

            return null; // we'll never get here, but anyway
        }

        public override object Visit(LiteralNode node)
        {
            Console.WriteLine($"visiting literal {node}, parsing {node.Type}, {node.Token.Content}");
            return _symbolTable.ParseResult(node.Type, node.Token.Content);
        }

        public override object Visit(VariableNode node)
        {
            var id = node.Token.Content;
            if (!_symbolTable.SymbolExists(id))
            {
                ThrowError(ErrorType.UndeclaredVariable,
                    node,
                    $"{id} not declared");
            }

            return _symbolTable.LookupValue(id);
        }

        // public override object Visit(ErrorNode node)
        // {
        //     Console.WriteLine("I am in errornode visitor?");
        //     return null; 
        // }
    }
}