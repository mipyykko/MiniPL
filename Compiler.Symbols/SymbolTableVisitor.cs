using System;
using System.Linq;
using Compiler.Common;
using Compiler.Common.AST;
using Compiler.Interpret;

namespace Compiler.Symbols
{
    public class SymbolTableVisitor : Visitor
    {
        private SymbolTable _symbolTable;
        public SymbolTableVisitor(SymbolTable symbolTable)
        {
            _symbolTable = symbolTable;
        }

        public override object Visit(StatementNode node)
        {
            node.Arguments.Select(n => n.Accept(this));
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
            node.Left.Accept(this);
            node.Right.Accept(this);
            return null;
        }

        public override object Visit(UnaryNode node)
        {
            node.Accept(this);
            node.Value.Accept(this);
            return null;
        }

        public override object Visit(AssignmentNode node)
        {
            var id = node.Id.Token.Content;
            var type = node.Type;
            node.Expression.Accept(this);

            if (node.Token?.KeywordType != KeywordType.Var)
            {
                if (!_symbolTable.SymbolExists(id))
                {
                    throw new Exception($"variable {id} not declared");
                }

                if (_symbolTable.IsControlVariable(id))
                {
                    throw new Exception($"attempting to assign to control variable {id}");
                }
            }
            else
            {
                if (_symbolTable.SymbolExists(id))
                {
                    throw new Exception($"attempting to redeclare variable {id}");
                }
            }
            _symbolTable.DeclareSymbol(id, node.Type);

            return null;
        }

        public override object Visit(VariableNode node)
        {
            var id = node.Token.Content;

            if (!_symbolTable.SymbolExists(id))
            {
                throw new Exception($"variable {id} not declared");   
            }

            return null;
        }

        public override object Visit(LiteralNode node)
        {
            return null;
        }

        public override object Visit(NoOpNode node)
        {
            return null;
        }

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
            node.Expression.Accept(this);
            return null;
        }
    }
}