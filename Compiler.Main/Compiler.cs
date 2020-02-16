using System;
using Compiler.Scan;
using Text = Compiler.Common.Text;
using Parse;

namespace Compiler.Main
{
    public class Compiler
    {
        public Compiler(string source)
        {
            Scanner scanner = new Scanner(new Text(source));
            Parser parse = new Parser(scanner);
            parse.Program();
        }
    }
}
