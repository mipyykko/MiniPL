using System;
using Compiler.Common;
using Compiler.Symbols;
using Node = Compiler.Common.AST.Node;

namespace Compiler.Interpret
{
    public class Interpreter
    {
        private Node _tree;
        private SymbolTable _symbolTable;
        
        public Interpreter(Node tree, Text source, SymbolTable symbolTable)
        {
            _tree = tree;
            _symbolTable = symbolTable;
            Visitor v = new ProgramVisitor(_symbolTable, source);
            _tree.Accept(v);
        }
    }
}