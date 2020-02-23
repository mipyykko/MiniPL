using System;
using System.Collections.Generic;
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
        public object LatestResult;
        
        public ProgramVisitor() {
        }


        private void UpdateSymbol(string id, PrimitiveType type, object value)
        {
            if (value == null)
            {
                SymbolTable[id] = (type, null);
                return;
            }
            
            switch (type)
            {
                case PrimitiveType.Int:
                    SymbolTable[id] = (PrimitiveType.Int, int.Parse((string) value));
                    break;
                case PrimitiveType.String:
                    SymbolTable[id] = (PrimitiveType.String, (string) value);
                    break;
                case PrimitiveType.Bool:
                    SymbolTable[id] = (PrimitiveType.Bool, ((string) value).ToLower().Equals("true"));
                    break;
            }
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

        public override void Visit(StatementNode node)
        {
            Console.WriteLine("statement");
            switch (node.Function)
            {
                case FunctionType.Print:
                    node.Arguments[0].Accept(this);
                    Console.Write(LatestResult);
                    break;
                case FunctionType.Assert:
                    node.Arguments[0].Accept(this);
                    if ((bool) LatestResult != true)
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
                case FunctionType.Var:
                    
                    break;
                default:
                    break;
            }
        }

        public override void Visit(NoOpNode node)
        {
            return;
        }
        
        public override void Visit(StatementListNode node)
        {
            Console.WriteLine("statementlist {0}", node);
            node.Left.Accept(this);
            node.Right.Accept(this);
        }

        public override void Visit(BinaryNode node)
        {   
            node.Left.Accept(this);
            var opnd1 = LatestResult;
            node.Right.Accept(this);
            var opnd2 = LatestResult;

            switch (node.Op.Type)
            {
                case TokenType.Addition:
                    LatestResult = (int) opnd1 + (int) opnd2;
                    break;
                case TokenType.Subtraction:
                    LatestResult = (int) opnd1 - (int) opnd2;
                    break;
                case TokenType.Multiplication:
                    LatestResult = (int) opnd1 * (int) opnd2;
                    break;
                case TokenType.Division:
                    LatestResult = (int) opnd1 / (int) opnd2;
                    break;
                case TokenType.And:
                    LatestResult = (bool) opnd1 && (bool) opnd2;
                    break;
                case TokenType.LessThan:
                    LatestResult = (int) opnd1 < (int) opnd2;
                    break;
                case TokenType.Equals:
                    LatestResult = opnd1 == opnd2;
                    break;
            }
            Console.WriteLine("node {0} result {1}", node, LatestResult);
        }

        public override void Visit(UnaryNode node)
        {
            node.Value.Accept(this);
            LatestResult = !((bool) LatestResult);
        }

        public override void Visit(AssignmentNode node)
        {
            var id = node.Token.Content;
            node.Value.Accept(this);
            var value = LatestResult;
            var type = PrimitiveType.Unknown;

            if (node.Declaration)
            {
                if (SymbolTable.ContainsKey(id))
                {
                    throw new Exception($"variable {id} already defined");
                }

                type = node.GivenType;
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
            Console.WriteLine($"modified {id} {type} {value}");
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

        public void SetResult(PrimitiveType type, object value)
        {
            switch (type)
            {
                case PrimitiveType.Int:
                    LatestResult = int.Parse((string) value);
                    break;
                case PrimitiveType.String:
                    LatestResult = (string) value;
                    break;
                case PrimitiveType.Bool:
                    LatestResult = ((string) value).ToLower().Equals("true");
                    break;
                default:
                    break;
            }
        }
        public override void Visit(LiteralNode node)
        {
            Console.WriteLine("literal {0}", node.Value);

            if (node.Value == null)
            {
                LatestResult = 0;
            }
            else
            {
                SetResult(node.Type, node.Value);
            }
        }

        public override void Visit(VariableNode node)
        {
            var name = node.Token.Content;
            if (!SymbolTable.ContainsKey(name))
            {
                throw new Exception($"{name} not declared");
            }

            (var type, var value) = SymbolTable[name];
            LatestResult = value;

            Console.WriteLine("name {0}", node.Token.Content);
        }

        // public override void Visit(EOFNode node)
        // {
        //     Console.WriteLine("done");
        // }
    }
}