using System;
using System.Collections.Generic;

namespace Compiler.Common
{
    public static class Util
    {
        public static U TryGetValueOrDefault<T, U>(this Dictionary<T, U> dictionary, T key, U defaultValue = default(U))
        {
            if (dictionary == null) throw new ArgumentNullException();
            if (key == null) throw new ArgumentNullException();

            return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
        }

        public static bool Includes<T>(this T[] array, T value)
        {
            return Array.IndexOf(array, value) >= 0;
        }
        
        public static PrimitiveType ToPrimitiveType(this KeywordType kw)
        {
            switch (kw)
            {
                case KeywordType.Int:
                    return PrimitiveType.Int;
                case KeywordType.String:
                    return PrimitiveType.String;
                case KeywordType.Bool:
                    return PrimitiveType.Bool;
                case KeywordType.Assert:
                case KeywordType.Print:
                case KeywordType.Read:
                    return PrimitiveType.Void;
                default:
                    return PrimitiveType.Void;
            }
        }
        
        public static string Spaces(int n) => new string(' ', n); 
        
    }
}