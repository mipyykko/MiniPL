using System;
using Compiler.Common;
using Compiler.Common.Errors;
using Compiler.Interpret;
using Compiler.Scan;
using Text = Compiler.Common.Text;
using Compiler.Parse;

namespace Compiler.Main
{
    public class Compiler
    {
        public Compiler(string source)
        {
            Context.Source = Text.Of(source);
            Context.ErrorService = new ErrorService();
            Context.SymbolTable = new SymbolTable();
            
            var scanner = new Scanner();
            var parser = new Parser(scanner);
            var tree = parser.Program();
            var symbolTableVisitor = new SymbolTableVisitor();
            tree.Accept(symbolTableVisitor);
            
            tree.AST();
            new Interpreter(tree);
            
            Context.ErrorService.Throw();
        }
    }
}