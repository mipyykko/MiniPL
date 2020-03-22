using System;
using System.IO;
using MiniPL.Common;

namespace MiniPL.Main
{
    internal static class Program
    {
        private static string Help = @$"Mini-PL Interpreter

Usage: <executable name> [options] filename

Options: 

--ast: Output decorated AST.";
            
        public static void Main(string[] args)
        {
            var ast = false;
            var filename = "";

            foreach (var arg in args)
            {
                if (arg.StartsWith("--") && arg.Trim().ToLower().Contains("ast")) ast = true;
                if (!arg.StartsWith("--")) filename = arg;
            }

            if (filename.Equals(""))
            {
                Console.WriteLine(Help);
                Environment.Exit(1);
            }
            Context.Options.AST = ast;

            var source = File.ReadAllText(filename);


            var _ = new Compiler(source);

            Console.WriteLine();
        }

    }

}