using Compiler.Common;
using Compiler.Common.AST;

namespace Compiler.Common
{
    public static class ErrorMessages
    {
        public static string ErrorMessage(ErrorType errorType, Token token, Node node, params object[] info) =>
            errorType switch
            {
                ErrorType.TypeError => $"type error: expected value of type {info[0]}, got {node.Type}",
                ErrorType.UndeclaredVariable => $"variable {info[0]} not declared",
                ErrorType.RedeclaredVariable => $"variable {info[0]} already declared",
                ErrorType.UninitializedVariable => $"variable {info[0]} used before assigment",
                ErrorType.SyntaxError => $"syntax error: expected {info[0]}, got {token.Content}",
                _ => $"unknown error"
            };
    }
}