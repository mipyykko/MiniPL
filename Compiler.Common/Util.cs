using System;
using System.Collections.Generic;
using System.Linq;

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
        public static PrimitiveType GuessType(string value)
        {
            if (value == null) return PrimitiveType.Void;
            if (value[0] == '-' && value.Substring(1).ToCharArray().All(char.IsDigit)) return PrimitiveType.Int;
            if (value.ToCharArray().All(char.IsDigit)) return PrimitiveType.Int;
            return PrimitiveType.String;
        }
        
        public static void Deconstruct<T0>(this object[] items, out T0 t0)
        {
            t0 = items.Length > 0 ? (T0) items[0] : default;
        }

        public static void Deconstruct<T0, T1>(this object[] items, out T0 t0, out T1 t1)
        {
            t0 = items.Length > 0 ? (T0) items[0] : default;
            t1 = items.Length > 1 ? (T1) items[1] : default;
        }
        
        public static void Deconstruct<T0, T1, T2>(this object[] items, out T0 t0, out T1 t1, out T2 t2)
        {
            t0 = items.Length > 0 ? (T0) items[0] : default;
            t1 = items.Length > 1 ? (T1) items[1] : default;
            t2 = items.Length > 2 ? (T2) items[2] : default;
        }

        public static void Deconstruct<T0, T1, T2, T3>(this object[] items, out T0 t0, out T1 t1, out T2 t2, out T3 t3)
        {
            t0 = items.Length > 0 ? (T0) items[0] : default;
            t1 = items.Length > 1 ? (T1) items[1] : default;
            t2 = items.Length > 2 ? (T2) items[2] : default;
            t3 = items.Length > 3 ? (T3) items[3] : default;
        }
        
        public static void Deconstruct<T0, T1, T2, T3, T4>(this object[] items, out T0 t0, out T1 t1, out T2 t2, out T3 t3, out T4 t4)
        {
            t0 = items.Length > 0 ? (T0) items[0] : default;
            t1 = items.Length > 1 ? (T1) items[1] : default;
            t2 = items.Length > 2 ? (T2) items[2] : default;
            t3 = items.Length > 3 ? (T3) items[3] : default;
            t4 = items.Length > 4 ? (T4) items[4] : default;
        }
        
        public static void Deconstruct<T0, T1, T2, T3, T4, T5>(this object[] items, out T0 t0, out T1 t1, out T2 t2, out T3 t3, out T4 t4, out T5 t5)
        {
            t0 = items.Length > 0 ? (T0) items[0] : default;
            t1 = items.Length > 1 ? (T1) items[1] : default;
            t2 = items.Length > 2 ? (T2) items[2] : default;
            t3 = items.Length > 3 ? (T3) items[3] : default;
            t4 = items.Length > 4 ? (T4) items[4] : default;
            t5 = items.Length > 5 ? (T5) items[5] : default;
        }
    }
}