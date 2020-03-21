using System;
using System.Collections.Generic;
using System.Net.Mime;

namespace MiniPL.Common.Errors
{
    public class ErrorService : IErrorService
    {
        private static Text Source => Context.Source;
        private static List<Error> _errors = new List<Error>();

        public bool HasErrors => _errors.Count > 0;
        
        public bool Add(ErrorType type, Token token, string message, bool critical = false)
        {
            _errors.Add(Error.Of(
                type,
                token,
                message
            ));

            if (critical)
            {
                Throw();
            }
            return true;
        }

        public void Throw()
        {
            if (!HasErrors) return;
            
            foreach (var error in _errors)
            {
                var errorLine = error.Token.SourceInfo.LineRange.Line;
                Console.WriteLine($"\nError:\n======\n{error.Message} on line {errorLine}:");
                for (var i = Math.Max(0, errorLine - 2); i < Math.Min(Source.Lines.Count, errorLine + 3); i++)
                {
                    Console.WriteLine($"{i}: {Source.Lines[i]}");

                }

            }
            Environment.Exit(1);
        }
    }
}