using System;
namespace Compiler.Common
{
    public class ValueType<T>
    {
        public T Value { get; private set; }

        public ValueType(T value)
        {
            Value = value;
        }
    }
}
