using Compiler.Common;

namespace Compiler.Interpret
{
    public interface IProgramMemory
    {
        ErrorType UpdateVariable(string id, object value = null, bool control = false);
        ErrorType UpdateControlVariable(string id, object value);
        object LookupVariable(string id);
        object ParseResult(PrimitiveType type, object value);
    }
}