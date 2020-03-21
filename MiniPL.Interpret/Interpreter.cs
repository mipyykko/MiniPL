using MiniPL.Common;
using MiniPL.Common.Symbols;
using Node = MiniPL.Common.AST.Node;

namespace MiniPL.Interpret
{
    public class Interpreter
    {
        public Interpreter(Node tree)
        {
            var v = new ProgramVisitor(new ProgramMemory());
            tree.Accept(v);
        }
    }
}