using System;
using System.Linq;
using MiniPL.Common;
using MiniPL.Common.AST;
using MiniPL.Common.Errors;
using MiniPL.Common.Symbols;
using MiniPL.Interpret;

namespace MiniPL.Interpret
{
    public class SemanticAnalysisVisitor : Visitor
    {
        private ISymbolTable SymbolTable => Context.SymbolTable;
        private IErrorService ErrorService => Context.ErrorService;

        private static readonly OperatorType[] BooleanOperators = new[]
        {
            OperatorType.And,
            OperatorType.Equals,
            OperatorType.Not,
            OperatorType.LessThan
        };

        public override object Visit(StatementNode node)
        {
            switch (node.Token.KeywordType)
            {
                case KeywordType.Print:
                {
                    var type = node.Arguments[0].Accept(this);

                    if (type == PrimitiveType.Int || type == PrimitiveType.String) return type;
                    
                    ErrorService.Add(
                        ErrorType.TypeError,
                        node.Arguments[0].Token,
                        $"can only print Int or String types, got {type}"
                    );
                    return null;

                }
                case KeywordType.Assert:
                {
                    var type = node.Arguments[0].Accept(this);

                    if (type == PrimitiveType.Bool) return type;

                    ErrorService.Add(
                        ErrorType.TypeError,
                        node.Arguments[0].Token,
                        $"can only assert Boolean expressions, got {type}"
                    );
                    return null;
                }
                case KeywordType.Read:
                {
                    var id = ((VariableNode) node.Arguments[0]).Token.Content;
                    var type = node.Arguments[0].Accept(this);

                    if (type != PrimitiveType.Int && type != PrimitiveType.String)
                    {
                        ErrorService.Add(
                            ErrorType.TypeError,
                            node.Arguments[0].Token,
                            $"can only read Int or String types, got {type}"
                        );
                        return null;
                    }

                    if (SymbolTable.IsControlVariable(id))
                    {
                        ErrorService.Add(
                            ErrorType.AssignmentToControlVariable,
                            node.Arguments[0].Token,
                            $"can't assign read result to control variable {id}",
                            true
                        );
                        return null;
                    }

                    return type; 
                }
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

            var op = Token.OperatorToOperatorType.TryGetValueOrDefault(node.Token.Content);

            if (type1 != type2)
            {
                ErrorService.Add(
                    ErrorType.TypeError, 
                    node.Token,
                    $"type error: can't perform operation {node.Token.Content} on {type1} and {type2}"
                );
            }

            var type = BooleanOperators.Includes(op) ? PrimitiveType.Bool : type1;
            
            node.Operator = op;
            node.Type = type;
            return type;
        }

        public override object Visit(UnaryNode node)
        {
            var op = Token.OperatorToOperatorType.TryGetValueOrDefault(node.Token.Content);
            var type = (PrimitiveType) node.Value.Accept(this);
            
            node.Operator = op;

            if ((op == OperatorType.Not && type != PrimitiveType.Bool) ||
                (op == OperatorType.Subtraction && type != PrimitiveType.Int))
            {
                ErrorService.Add(
                    ErrorType.TypeError, 
                    node.Token,
                    $"type error: can't perform operation {node.Token.Content} on {type}"
                );
            }
            node.Type = op == OperatorType.Not ? PrimitiveType.Bool : type;
            return type;
        }

        public override object Visit(AssignmentNode node)
        {
            var id = node.Id.Token.Content;
            var type = node.Type;
            node.Expression.Accept(this);

            if (node.Token?.KeywordType != KeywordType.Var)
            {
                if (!SymbolTable.SymbolExists(id))
                {
                    ErrorService.Add(
                        ErrorType.UndeclaredVariable, 
                        node.Id.Token, 
                        $"variable {id} not declared"
                    );
                }

                if (SymbolTable.IsControlVariable(id))
                {
                    ErrorService.Add(
                        ErrorType.AssignmentToControlVariable, 
                        node.Id.Token,
                        $"attempting to assign to control variable {id}"
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
                        $"type error: attempting to assign {node.Expression.Type} to variable {id} of type {node.Id.Type}"
                    );
                }
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
            return node.Expression.Accept(this);
        }
    }
}