using Compiler.Interpret;

namespace Compiler.Common
{
    public interface IVisitable
    {
        public abstract void Accept(Visitor visitor);
    }
}