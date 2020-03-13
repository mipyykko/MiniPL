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
        AssignmentToControlVariable,
        InvalidRange,
        InvalidOperation
    }
}