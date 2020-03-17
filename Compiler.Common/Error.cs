using System;
using Compiler.Common;

namespace Compiler.Parse
{
    public class Error
    {
        private ErrorType _errorType;
        private readonly string _message;
        private readonly Token _token;

        private Error(ErrorType type, Token token, string message)
        {
            _errorType = type;
            _token = token;
            _message = message;
        }

        public static Error Of(ErrorType type, string message) => new Error(type, null, message);
        public static Error Of(ErrorType type, Token token, string message) => new Error(type, token, message);

        public override string ToString()
        {
            return _message;
        }

        public void Throw()
        {
            var errorLine = _token.SourceInfo.LineRange.Line;
            Console.WriteLine($"\nError: {_message} on line {errorLine}:");
            // for (var i = Math.Max(0, errorLine - 2); i < Math.Min(_source.Lines.Count, errorLine + 3); i++)
            // {
            //     Console.WriteLine($"{i}: {_source.Lines[i]}");
            //     
            // }
            Environment.Exit(1);
            
        }
    }
}