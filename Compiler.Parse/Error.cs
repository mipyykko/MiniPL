using Compiler.Common;

namespace Compiler.Parse
{
    public class Error
    {
        private ErrorType _errorType;
        private string _message;

        private Error(ErrorType type, string message)
        {
            _errorType = type;
            _message = message;
        }

        public static Error Of(ErrorType type, string message) => new Error(type, message);

        public override string ToString()
        {
            return _message;
        }
    }
}