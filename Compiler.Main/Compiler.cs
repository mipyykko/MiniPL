using System;
using Compiler.Interpret;
using Compiler.Scan;
using Text = Compiler.Common.Text;
using Compiler.Parse;
using Compiler.Symbols;

namespace Compiler.Main
{
    public class Compiler
    {
        public Compiler(string source)
        {
            var sourceText = Text.Of(source);
            var scanner = new Scanner(sourceText);
            var parser = new Parser(scanner);
            var tree = parser.Program();
            var symbolTable = new SymbolTable();
            var symbolTableVisitor = new SymbolTableVisitor(symbolTable);
            tree.Accept(symbolTableVisitor);
            
            tree.AST();
            if (parser.Errors.Count > 0)
            {
                foreach (var error in parser.Errors)
                {
                    Console.WriteLine(error);
                }
            }
            new Interpreter(tree, sourceText, symbolTable);
        }
    }
}