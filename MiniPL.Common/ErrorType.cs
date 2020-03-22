namespace MiniPL.Common
{
    public enum ErrorType
    {
        Unknown,
        SyntaxError,
        ParseError,
        TypeError,
        AssertionError,
        UnexpectedKeyword,
        UnexpectedToken,
        UndeclaredVariable,
        RedeclaredVariable,
        UninitializedVariable,
        AssignmentToControlVariable,
        InvalidRange,
        InvalidOperation,
        UnterminatedStringTerminal,
        InputError
        
    }
}