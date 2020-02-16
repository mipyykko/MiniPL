using System;
using System.Collections.Generic;

namespace Compiler.Common
{
    public static class Util
    {
        public static U TryGetValueOrDefault<T, U>(this Dictionary<T, U> dictionary, T key, U defaultValue = default(U))
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException();
            }
            if (key == null)
            {
                throw new ArgumentNullException();
            }

            return dictionary.TryGetValue(key, out U value) ? value : defaultValue;
        }
    }
}
