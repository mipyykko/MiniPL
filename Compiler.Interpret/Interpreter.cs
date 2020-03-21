using Compiler.Common;
using Compiler.Common.Symbols;
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
            var v = new ProgramVisitor(new ProgramMemory());
            _tree.Accept(v);
        }
    }
}