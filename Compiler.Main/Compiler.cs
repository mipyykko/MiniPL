using System;
using Compiler.Scan;
using Text = Compiler.Common.Text;
namespace Compiler.Main
{
    public class Compiler
    {
        public Compiler(string source)
        {
            Scanner scanner = new Scanner(new Text(source));
            Console.WriteLine(scanner.Scan());
        }
    }
}
