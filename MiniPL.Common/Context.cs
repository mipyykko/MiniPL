using MiniPL.Common.Errors;
using MiniPL.Common.Symbols;

namespace MiniPL.Common
{
    public static class Context
    {
        public static Text Source { get; set; }
        public static IErrorService ErrorService { get; set; }
        public static ISymbolTable SymbolTable { get; set; }
    }
}