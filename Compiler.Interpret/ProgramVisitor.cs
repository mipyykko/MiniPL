using System;
using System.Collections.Generic;
using System.Diagnostics;
using Compiler.Common;
using Compiler.Common.AST;
using Node = Compiler.Common.AST.Node;

namespace Compiler.Interpret
{
    public class ProgramVisitor : Visitor
    {
        private Text _source;

        public ProgramVisitor(Text source) => _source = source;
        
        Dictionary<string, (PrimitiveType, object)> SymbolTable = new Dictionary<string, (PrimitiveType, object)>();
        Dictionary<string, bool> ControlVariables = new Dictionary<string, bool>();
        

        private ErrorType UpdateSymbol(string id, PrimitiveType type, object value, bool control = false)
        {
            if (!control && ControlVariables.TryGetValueOrDefault(id))
            {
                return ErrorType.AssignmentToControlVariable;
            }

            SymbolTable[id] = (type, ParseResult(type, value));
            return ErrorType.Unknown;
        }

        private ErrorType UpdateSymbol(string id, object value, bool control = false)
        {
            return UpdateSymbol(id, SymbolTable.TryGetValueOrDefault(id).Item1, value, control);
        }

        private ErrorType UpdateControlVariable(string id, object value)
        {
            return UpdateSymbol(id, value, true);
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
                    var value = Console.ReadLine();
                    var id = ((VariableNode) node.Arguments[0]).Token.Content;

                    UpdateSymbol(id, value);
                    break;
                }
                default:
                    break;
            }

            return null;
        }

        public override object Visit(ForNode node)
        {
            var id = node.Token.Content;
            var rangeStart = node.RangeStart.Accept(this);
            var rangeEnd = node.RangeEnd.Accept(this);
            var error = ErrorType.Unknown;
            
            if (!(rangeStart is int) || !(rangeEnd is int))
            {
                ThrowError(ErrorType.InvalidRange,
                    node,
                    $"invalid range {rangeStart}..{rangeEnd}");
            }

            if (!SymbolTable.ContainsKey(id))
            {
                ThrowError(ErrorType.UndeclaredVariable,
                    node,
                    $"control variable {id} not declared");
            }

            error = UpdateSymbol(id, (int) rangeStart);
            if (error != ErrorType.Unknown)
            {
                ThrowError(error,
                    node,
                    $"unable to assign to control variable {id}");
            }
            ControlVariables[id] = true;
            var i = (int) rangeStart;

            while (i <= (int) rangeEnd)
            {
                node.Statements.Accept(this);
                i++;
                error = UpdateControlVariable(id, i);
                if (error != ErrorType.Unknown)
                {
                    ThrowError(error,
                        node,
                        $"unable to assign to control variable {id}");
                }
            }

            ;
            ControlVariables[id] = false;

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
            var opnd1 = node.Left.Accept(this);
            var opnd2 = node.Right.Accept(this);
            var op = ToOperatorType.TryGetValueOrDefault(node.Token.Content);

            switch (op)
            {
                case OperatorType.Addition when opnd1 is int o1 && opnd2 is int o2:
                    return o1 + o2;
                case OperatorType.Addition when opnd1 is string o1 && opnd2 is string o2:
                    return o1 + o2;
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
            var type = PrimitiveType.Void;

            if (node.Token?.KeywordType == KeywordType.Var)
            {
                if (SymbolTable.ContainsKey(id))
                {
                    ThrowError(ErrorType.RedeclaredVariable,
                        node,
                        $"variable {id} already defined");
                }

                type = node.Type;
            }
            else
            {
                if (!SymbolTable.ContainsKey(id))
                {
                    ThrowError(ErrorType.UndeclaredVariable,
                        node,
                        $"variable {id} not declared");
                }

                type = SymbolTable[id].Item1;
            }

            UpdateSymbol(id, type, value);
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

        public object ParseResult(PrimitiveType type, object value) =>
            type switch
            {
                PrimitiveType.Int when value is string s => int.Parse(s),
                PrimitiveType.String => (string) value,
                PrimitiveType.Bool when value is string s => s.ToLower().Equals("true"),
                _ => value
            };

        public override object Visit(LiteralNode node)
        {
            return ParseResult(node.Type, node.Token.Content);
        }

        public override object Visit(VariableNode node)
        {
            var name = node.Token.Content;
            if (!SymbolTable.ContainsKey(name))
            {
                ThrowError(ErrorType.UndeclaredVariable,
                    node,
                    $"{name} not declared");
            }

            var ( _,  value) = SymbolTable[name];
            return value;
        }

        // public override object Visit(ErrorNode node)
        // {
        //     Console.WriteLine("I am in errornode visitor?");
        //     return null; 
        // }
    }
}