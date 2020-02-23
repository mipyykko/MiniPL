using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Compiler.Common;
using Compiler.Common.AST;
using Node = Compiler.Common.AST.Node;

namespace Compiler.Interpret
{
    public class ProgramVisitor : Visitor
    {
        Dictionary<string, (PrimitiveType, object)> SymbolTable = new Dictionary<string, (PrimitiveType, object)>(); 
        
        public ProgramVisitor() {
        }


        private void UpdateSymbol(string id, PrimitiveType type, object value)
        {
            if (value == null)
            {
                SymbolTable[id] = (type, null);
                return;
            }

            var val = ParseResult(type, value);
            SymbolTable[id] = (type, val);
            // switch (type)
            // {
            //     case PrimitiveType.Int:
            //         SymbolTable[id] = (PrimitiveType.Int, int.Parse((string) value));
            //         break;
            //     case PrimitiveType.String:
            //         SymbolTable[id] = (PrimitiveType.String, (string) value);
            //         break;
            //     case PrimitiveType.Bool:
            //         SymbolTable[id] = (PrimitiveType.Bool, ((string) value).ToLower().Equals("true"));
            //         break;
            // }
        }

        private void UpdateSymbol(string id, object value)
        {
            UpdateSymbol(id, SymbolTable[id].Item1, value);
        }
        
        // public override void Visit(NameNode node)
        // {
        //     return;
        // }
        //
        // public override void Visit(ProgramNode node)
        // {
        //     Console.WriteLine("program");
        //     node.Left.Accept(this);
        // }

        public override object Visit(StatementNode node)
        {
            Console.WriteLine("statement");
            switch (node.Function)
            {
                case FunctionType.Print:
                    Console.Write(node.Arguments[0].Accept(this));
                    break;
                case FunctionType.Assert:
                    var result = (bool) node.Arguments[0].Accept(this);
                    if (result != true)
                    {
                        throw new Exception("assertion failed");
                    }

                    break;
                case FunctionType.Read:
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
            var id = node.Token;
            var rangeStart = node.RangeStart.Accept(this);
            var rangeEnd = node.RangeEnd.Accept(this);
            
            if (!(rangeStart is int) || !(rangeEnd is int))
            {
                throw new Exception("invalid range");
            }

            int i = 0;

            do
            {
                UpdateSymbol(id.Content, i);
                node.Statements.Accept(this);
                i++;
            } while (i <= (int) rangeEnd + 1);

            
            Debug.WriteLine($"got to {SymbolTable[id.Content]}");
            return null;
        }
        
        public override object Visit(NoOpNode node)
        {
            return null;
        }
        
        public override object Visit(StatementListNode node)
        {
            Console.WriteLine("statementlist {0}", node);
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

        public override object Visit(BinaryNode node)
        {   
            var opnd1 = node.Left.Accept(this);
            var opnd2 = node.Right.Accept(this);
            var op = ToOperatorType.TryGetValueOrDefault(node.Op.Content);

            switch (op)
            {
                case OperatorType.Addition when opnd1 is int && opnd2 is int:
                    return (int) opnd1 + (int) opnd2;
                case OperatorType.Addition when opnd1 is string && opnd2 is string:
                    return (string) opnd1 + (string) opnd2;
                case OperatorType.Addition:
                    throw new Exception("invalid operation"); // TODO: generalize
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
            return node.Expression.Accept(this);
        }

        public override object Visit(AssignmentNode node)
        {
            var id = node.Token.Content;
            var value = node.Expression.Accept(this);
            var type = PrimitiveType.Void;

            if (node.Declaration)
            {
                if (SymbolTable.ContainsKey(id))
                {
                    throw new Exception($"variable {id} already defined");
                }

                type = node.Type;
            }
            else
            {
                if (!SymbolTable.ContainsKey(id))
                {
                    throw new Exception($"variable {id} not declared");
                }
                type = SymbolTable[id].Item1;
            }

            UpdateSymbol(id, type, value);
            Debug.WriteLine($"modified {id} {type} {value}");

            return value;
        }

        // public override void Visit(VarDeclarationNode node)
        // {
        //     var id = node.Name;
        //     var type = node.DeclaredType;
        //     node.Value.Accept(this);
        //     var value = LatestResult;
        //
        //     if (SymbolTable.ContainsKey(id))
        //     {
        //         throw new Exception($"variable {id} already defined");
        //     }
        //     
        //     SymbolTable.Add(id, (type, value));
        //     Console.WriteLine($"assigned {id} {type} {value}");
        // }

        public object ParseResult(PrimitiveType type, object value)
        {
            switch (type)
            {
                case PrimitiveType.Int when value is string:
                    return int.Parse((string) value);
                case PrimitiveType.String:
                    return (string) value;
                case PrimitiveType.Bool when value is string:
                    return ((string) value).ToLower().Equals("true");
                default:
                    return value;
            }
        }
        public override object Visit(LiteralNode node)
        {
            Console.WriteLine("literal {0}", node.Value);

            return ParseResult(node.Type, node.Value);
        }

        public override object Visit(VariableNode node)
        {
            var name = node.Token.Content;
            if (!SymbolTable.ContainsKey(name))
            {
                throw new Exception($"{name} not declared");
            }

            (var type, var value) = SymbolTable[name];
            return value;
        }

        // public override void Visit(EOFNode node)
        // {
        //     Console.WriteLine("done");
        // }
    }
}