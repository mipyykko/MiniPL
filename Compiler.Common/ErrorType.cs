namespace Compiler.Common
{
    public enum ErrorType
    {
        Unknown,
        SyntaxError,
        ParseError,
        TypeError,
        UnexpectedKeyword,
        UnexpectedToken,
        UndeclaredVariable,
        RedeclaredVariable,
        UninitializedVariable,
        AssignmentToControlVariable,
        InvalidRange,
        InvalidOperation
    }
}