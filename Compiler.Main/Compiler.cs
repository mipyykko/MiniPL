using System;
using Compiler.Common.AST;
using Compiler.Interpret;
using Compiler.Scan;
using Text = Compiler.Common.Text;
using Parse;

namespace Compiler.Main
{
    public class Compiler
    {
        public Compiler(string source)
        {
            var scanner = new Scanner(Text.Of(source));
            var parse = new Parser(scanner);
            Node tree = parse.Program();
            new Interpreter(tree);
        }
    }
}