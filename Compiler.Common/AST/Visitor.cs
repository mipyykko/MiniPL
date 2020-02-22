using System;
using Compiler.Common.AST;

namespace Compiler.Interpret
{
    public abstract class Visitor
    {
        public abstract void Visit(ProgramNode node);
        public abstract void Visit(StatementNode node);
        public abstract void Visit(StatementListNode node);
        public abstract void Visit(OperatorNode node);
        public abstract void Visit(BinaryNode node);
        public abstract void Visit(UnaryNode node);
        public abstract void Visit(VarAssignmentNode node);
        public abstract void Visit(VarDeclarationNode node);
        public abstract void Visit(LiteralNode node);
        public abstract void Visit(NameNode node);
        public abstract void Visit(EOFNode node);
        public abstract void Visit(IdentifierNode node);
    }

}