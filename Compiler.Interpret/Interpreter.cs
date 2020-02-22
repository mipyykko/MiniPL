using System;
using System.Collections.Generic;
using System.Diagnostics;
using Compiler.Common;
using Compiler.Common.AST;
using Node = Compiler.Common.AST.Node;

namespace Compiler.Interpret
{
    public class Interpreter
    {
        private Node tree;
        private Dictionary<string, string> symbolTable = new Dictionary<string,string>();

        public Interpreter(Node tree)
        {
            this.tree = tree;
            Visitor v = new ProgramVisitor();
            tree.Accept(v);
        }

        /*public void Process(Node node)
        {
            switch (node.Type)
            {
                case Node.NodeType.Program:
                case Node.NodeType.Statement:
                case Node.NodeType.StatementList:
                    node.Children.ForEach(child => Process(child));
                    break;
                case Node.NodeType.VariableAssignment:
                {
                    var id = node.Children[0].Value;
                    var existing = symbolTable.TryGetValueOrDefault(id);
                    if (existing == null)
                    {
                        throw new Exception($"variable {id} not defined");
                    }

                    var value = EvaluateExpression(node.Children[1]);
                    symbolTable[id] = value;
                    break;
                }
                case Node.NodeType.VariableDeclaration:
                {
                    var id = node.Children[0].Value;
                    var existing = symbolTable.TryGetValueOrDefault(id);
                    if (existing != null)
                    {
                        throw new Exception($"variable {id} already defined");
                    }

                    var type = node.Children[1].Type;

                    string value = null;
                    
                    if (node.Children.Count > 2)
                    {
                        value = EvaluateExpression(node.Children[2]);
                    }

                    symbolTable[id] = value;
                    break;
                }


            }
        }

        public string EvaluateExpression(Node node)
        {
            switch (node.Type)
            {
                case Node.NodeType.IntValue:
                case Node.NodeType.StringValue:
                case Node.NodeType.BoolValue:
                    return node.Value;
                case Node.NodeType.ValueExpression:
                    return EvaluateExpression(node.Children[0]);
                case Node.NodeType.BinaryExpression:
                    var opnd1 = node.Children[0];
                    var op = node.Children[1];
                    var opnd2 = node.Children[2];

                    var opnd1value = EvaluateExpression(opnd1);
                    var opnd2value = EvaluateExpression(opnd2);
                    
                    switch (op.Type)
                    {
                        case Node.NodeType.Addition:
                            if (opnd1.Type == opnd2.Type && opnd1.Type == Node.NodeType.IntValue)
                            {
                                return (int.Parse(EvaluateExpression(opnd1)) + int.Parse(EvaluateExpression(opnd2))).ToString();
                            }
                    }
            }
        }*/
    }
}