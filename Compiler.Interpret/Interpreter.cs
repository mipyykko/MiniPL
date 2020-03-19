using System;
using Compiler.Common;
using Node = Compiler.Common.AST.Node;

namespace Compiler.Interpret
{
    public class Interpreter
    {
        private Node _tree;
        private SymbolTable _symbolTable;
        
        public Interpreter(Node tree)
        {
            _tree = tree;
            Visitor v = new ProgramVisitor();
            _tree.Accept(v);
        }
    }
}