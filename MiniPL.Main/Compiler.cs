using System;
using MiniPL.Common;
using MiniPL.Common.Errors;
using MiniPL.Common.Symbols;
using MiniPL.Interpret;
using MiniPL.Scan;
using Text = MiniPL.Common.Text;
using MiniPL.Parse;

namespace MiniPL.Main
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
            var semanticAnalysisVisitor = new SemanticAnalysisVisitor();
            tree.Accept(semanticAnalysisVisitor);
            
            if (Context.Options.AST) tree.AST();

            if (Context.ErrorService.HasErrors())
            {
                Context.ErrorService.Throw();
            }
            
            var _ = new Interpreter(tree);
            
        }
    }
}