using System;
using System.Linq;
using Compiler.Common;
using Compiler.Common.AST;
using Compiler.Interpret;
using static Compiler.Common.Util;

namespace Compiler.Symbols
{
    public class SymbolTableVisitor : Visitor
    {
        private readonly SymbolTable _symbolTable;
        
        public SymbolTableVisitor(SymbolTable symbolTable)
        {
            _symbolTable = symbolTable;
        }

        public override object Visit(StatementNode node)
        {
            switch (node.Token.KeywordType)
            {
                case KeywordType.Print:
                case KeywordType.Read:
                case KeywordType.Assert:
                    return node.Arguments[0].Accept(this);
            }
            return null;
        }

        public override object Visit(StatementListNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            return null;
        }

        public override object Visit(BinaryNode node)
        {
            var type1 = (PrimitiveType) node.Left.Accept(this);
            var type2 = (PrimitiveType) node.Right.Accept(this);

            if (type1 != type2)
            {
                throw new Exception($"type error: can't perform operation {node.Token.Content} on {type1} and {type2}");
            }

            node.Type = type1;
            return type1;
        }

        public override object Visit(UnaryNode node)
        {
            var type = (PrimitiveType) node.Value.Accept(this);
            return type;
        }

        public override object Visit(AssignmentNode node)
        {
            var id = node.Id.Token.Content;
            var type = node.Type;
            var expressionType = node.Expression.Accept(this);

            Console.WriteLine($"TYPE {type} EXPRESSION TYPE {node.Expression.Type} EXPRESSION {expressionType}");
            if (node.Token?.KeywordType != KeywordType.Var)
            {
                Console.WriteLine($"not var assignment: type is {type}, symbol {_symbolTable.LookupSymbol(id)}");
                if (!_symbolTable.SymbolExists(id))
                {
                    throw new Exception($"variable {id} not declared");
                }

                type = _symbolTable.LookupSymbol(id);
            }
            else
            {
                if (_symbolTable.SymbolExists(id))
                {
                    throw new Exception($"attempting to redeclare variable {id}");
                }
                if (!(node.Expression is NoOpNode) && node.Expression.Type != node.Id.Type)
                {
                    throw new Exception($"type error: attempting to assign {node.Expression.Type} to variable {id} of type {node.Id.Type}");
                }
            }
            if (_symbolTable.IsControlVariable(id))
            {
                throw new Exception($"attempting to assign to control variable {id}");
            }
            _symbolTable.DeclareSymbol(id, type);

            node.Type = type;
            node.Id.Type = type;

            return null;
        }

        public override object Visit(VariableNode node)
        {
            var id = node.Token.Content;

            if (!_symbolTable.SymbolExists(id))
            {
                throw new Exception($"variable {id} not declared");   
            }

            var type = _symbolTable.LookupSymbol(id);
            node.Type = type;
            return type;
        }

        public override object Visit(LiteralNode node)
        {
            Console.WriteLine($"LITERAL {node.Token} {node.Type}");
            return node.Type;
        }

        public override object Visit(NoOpNode node) => PrimitiveType.Void;

        public override object Visit(ForNode node)
        {
            var id = node.Token.Content;
            node.RangeStart.Accept(this);
            node.RangeEnd.Accept(this);

            if (_symbolTable.LookupSymbol(id) == null)
            {
                throw new Exception($"variable {id} not declared");
            }
            _symbolTable.SetControlVariable(id);
            node.Statements.Accept(this);
            _symbolTable.UnsetControlVariable(id);

            return null;
        }

        public override object Visit(ExpressionNode node)
        {
            var type = node.Expression.Accept(this);
            Console.WriteLine($"ExpressionNode got type {type}");
            return type;
        }
    }
}