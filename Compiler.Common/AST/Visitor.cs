using System;
using Compiler.Common.AST;

namespace Compiler.Interpret
{
    public abstract class Visitor
    {
        // public abstract void Visit(ProgramNode node);
        public abstract object Visit(StatementNode node);
        public abstract object Visit(StatementListNode node);
        // public abstract void Visit(OperatorNode node);
        public abstract object Visit(BinaryNode node);
        public abstract object Visit(UnaryNode node);
        public abstract object Visit(AssignmentNode node);
        // public abstract void Visit(VarAssignmentNode node);
        // public abstract void Visit(VarDeclarationNode node);
        public abstract object Visit(VariableNode node);
        public abstract object Visit(LiteralNode node);

        public abstract object Visit(NoOpNode node);
        // public abstract void Visit(NameNode node);
        // public abstract void Visit(EOFNode node);
        // public abstract void Visit(IdentifierNode node);
    }

}