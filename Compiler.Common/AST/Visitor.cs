using System;
using Compiler.Common.AST;

namespace Compiler.Interpret
{
    public abstract class Visitor
    {
        public abstract object Visit(StatementNode node);
        public abstract object Visit(StatementListNode node);
        public abstract object Visit(BinaryNode node);
        public abstract object Visit(UnaryNode node);
        public abstract object Visit(AssignmentNode node);
        public abstract object Visit(VariableNode node);
        public abstract object Visit(LiteralNode node);
        public abstract object Visit(NoOpNode node);
        public abstract object Visit(ForNode node);
        public abstract object Visit(ExpressionNode node);
    }

}