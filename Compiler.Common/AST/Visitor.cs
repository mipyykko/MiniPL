using System;
using Compiler.Common.AST;

namespace Compiler.Common.AST
{
    public abstract class Visitor
    {
        public abstract dynamic Visit(StatementNode node);
        public abstract dynamic Visit(StatementListNode node);
        public abstract dynamic Visit(BinaryNode node);
        public abstract dynamic Visit(UnaryNode node);
        public abstract dynamic Visit(AssignmentNode node);
        public abstract dynamic Visit(VariableNode node);
        public abstract dynamic Visit(LiteralNode node);
        public abstract dynamic Visit(NoOpNode node);
        public abstract dynamic Visit(ForNode node);
        public abstract dynamic Visit(ExpressionNode node);
    }

}