using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Compiler.Common;
using Compiler.Common.AST;
using Compiler.Symbols;
using Node = Compiler.Common.AST.Node;
using static Compiler.Common.Util;

namespace Compiler.Interpret
{
    public class ProgramVisitor : Visitor
    {
        private Text _source;
        private ProgramMemory _memory;
        private SymbolTable _symbolTable;
        
        public ProgramVisitor(SymbolTable symbolTable, Text source)
        {
            _symbolTable = symbolTable;
            _memory = new ProgramMemory(_symbolTable);
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
                    var value = Console.ReadLine();

                    _memory.UpdateVariable(id, value);
                    break;
                }
            }

            return null;
        }

        public override object Visit(ForNode node)
        {
            var id = node.Token.Content;
            var rangeStart = node.RangeStart.Accept(this);
            var rangeEnd = node.RangeEnd.Accept(this);

            _symbolTable.SetControlVariable(id);

            _memory.UpdateControlVariable(id, (int) rangeStart);

            var i = (int) rangeStart;

            while (i <= (int) rangeEnd)
            {
                node.Statements.Accept(this);
                i++;
                _memory.UpdateControlVariable(id, i);
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
                    ThrowError(ErrorType.InvalidOperation,
                        node,
                        $"invalid operation ${op}");
                    break;
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

            _memory.UpdateVariable(id, value ?? DefaultValue(type));

            return value;
        }

        public override object Visit(LiteralNode node)
        {
            return _memory.ParseResult(node.Type, node.Token.Content); // TODO: parsing in wrong place
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

            return _memory.LookupVariable(id);
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


    }
}