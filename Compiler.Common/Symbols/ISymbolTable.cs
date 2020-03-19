using Compiler.Common;

namespace Compiler.Common
{
    public interface ISymbolTable
    {
        ErrorType DeclareSymbol(string id, PrimitiveType type);
        PrimitiveType LookupSymbol(string id);
        bool SymbolExists(string id);
        ErrorType SetControlVariable(string id);
        ErrorType UnsetControlVariable(string id);
        bool IsControlVariable(string id);
    }
}