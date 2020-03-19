using System;
using System.Linq;
using Compiler.Common;
using Compiler.Common.AST;
using Compiler.Common.Errors;
using Compiler.Interpret;
using static Compiler.Common.Util;

namespace Compiler.Common
{
    public class SymbolTableVisitor : Visitor
    {
        private ISymbolTable SymbolTable => Context.SymbolTable;
        private IErrorService ErrorService => Context.ErrorService;

        public override object Visit(StatementNode node)
        {
            switch (node.Token.KeywordType)
            {
                case KeywordType.Print:
                case KeywordType.Assert:
                    return node.Arguments[0].Accept(this);
                case KeywordType.Read:
                    var id = ((VariableNode) node.Arguments[0]).Token.Content;

                    if (SymbolTable.IsControlVariable(id))
                    {
                        ErrorService.Add(
                            ErrorType.AssignmentToControlVariable, 
                            node.Arguments[0].Token,
                            $"can't assign read result to control variable {id}", 
                            true
                        );
                    }

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
                ErrorService.Add(
                    ErrorType.TypeError, 
                    node.Token,
                    $"type error: can't perform operation {node.Token.Content} on {type1} and {type2}"
                    );
            }

            node.Type = type1;
            return type1;
        }

        public override object Visit(UnaryNode node)
        {
            return (PrimitiveType) node.Value.Accept(this);
        }

        public override object Visit(AssignmentNode node)
        {
            var id = node.Id.Token.Content;
            var type = node.Type;
            var expressionType = node.Expression.Accept(this);

            Console.WriteLine($"TYPE {type} EXPRESSION TYPE {node.Expression.Type} EXPRESSION {expressionType}");
            if (node.Token?.KeywordType != KeywordType.Var)
            {
                Console.WriteLine($"not var assignment: type is {type}, symbol {SymbolTable.LookupSymbol(id)}");
                if (!SymbolTable.SymbolExists(id))
                {
                    ErrorService.Add(
                        ErrorType.UndeclaredVariable, 
                        node.Id.Token, 
                        $"variable {id} not declared"
                    );
                }

                type = SymbolTable.LookupSymbol(id);
            }
            else
            {
                if (SymbolTable.SymbolExists(id))
                {
                    ErrorService.Add(
                        ErrorType.RedeclaredVariable, 
                        node.Id.Token,
                        $"attempting to redeclare variable {id}"
                    );
                }

                if (!(node.Expression is NoOpNode) && node.Expression.Type != node.Id.Type)
                {
                    ErrorService.Add(
                        ErrorType.TypeError, 
                        node.Id.Token,
                        $"type error: attempting to assign {node.Expression.Type} {node.Expression.Token.Content} to variable {id} of type {node.Id.Type}"
                    );
                }
            }

            if (SymbolTable.IsControlVariable(id))
            {
                ErrorService.Add(
                    ErrorType.AssignmentToControlVariable, 
                    node.Id.Token,
                    $"attempting to assign to control variable {id}"
                );
            }

            SymbolTable.DeclareSymbol(id, type);

            node.Type = type;
            node.Id.Type = type;

            return null;
        }

        public override object Visit(VariableNode node)
        {
            var id = node.Token.Content;

            if (!SymbolTable.SymbolExists(id))
            {
                ErrorService.Add(
                    ErrorType.UndeclaredVariable,
                    node.Token,
                    $"variable {id} not declared"
                );
            }

            var type = SymbolTable.LookupSymbol(id);
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
            var id = node.Id.Token.Content;
            node.Id.Accept(this);
            node.RangeStart.Accept(this);
            node.RangeEnd.Accept(this);

            SymbolTable.SetControlVariable(id);
            node.Statements.Accept(this);
            SymbolTable.UnsetControlVariable(id);

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