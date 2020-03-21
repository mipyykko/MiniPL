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
        
        public static PrimitiveType ToPrimitiveType(this KeywordType kw) => 
            kw switch
            {
                KeywordType.Int => PrimitiveType.Int,
                KeywordType.String => PrimitiveType.String,
                KeywordType.Bool => PrimitiveType.Bool,
                KeywordType.Assert => PrimitiveType.Void,
                KeywordType.Print => PrimitiveType.Void,
                KeywordType.Read => PrimitiveType.Void,
                _ => PrimitiveType.Void
            };

        public static string Spaces(int n) => new string(' ', n);
        public static PrimitiveType TokenToPrimitiveType(TokenType tt) => 
            tt switch
            {
                TokenType.IntValue => PrimitiveType.Int,
                TokenType.StringValue => PrimitiveType.String,
                TokenType.BoolValue => PrimitiveType.Bool,
                _ => PrimitiveType.Void
            };

        public static PrimitiveType GuessType(string value)
        {
            if (value == null) return PrimitiveType.Void;
            if (value[0] == '-' && value.Substring(1).ToCharArray().All(char.IsDigit)) return PrimitiveType.Int;
            if (value.ToCharArray().All(char.IsDigit)) return PrimitiveType.Int;
            return PrimitiveType.String;
        }
        
        public static dynamic DefaultValue(PrimitiveType pt) => pt switch
        {
            PrimitiveType.Bool => false,
            PrimitiveType.Int => 0,
            PrimitiveType.String => "",
            _ => null
        };

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
        
        public static void Deconstruct<T0, T1, T2, T3, T4, T5, T6>(this object[] items, out T0 t0, out T1 t1, out T2 t2, out T3 t3, out T4 t4, out T5 t5, out T6 t6)
        {
            t0 = items.Length > 0 ? (T0) items[0] : default;
            t1 = items.Length > 1 ? (T1) items[1] : default;
            t2 = items.Length > 2 ? (T2) items[2] : default;
            t3 = items.Length > 3 ? (T3) items[3] : default;
            t4 = items.Length > 4 ? (T4) items[4] : default;
            t5 = items.Length > 5 ? (T5) items[5] : default;
            t6 = items.Length > 6 ? (T6) items[6] : default;
        }
        
        public static void Deconstruct<T0, T1, T2, T3, T4, T5, T6, T7>(this object[] items, out T0 t0, out T1 t1, out T2 t2, out T3 t3, out T4 t4, out T5 t5, out T6 t6, out T7 t7)
        {
            t0 = items.Length > 0 ? (T0) items[0] : default;
            t1 = items.Length > 1 ? (T1) items[1] : default;
            t2 = items.Length > 2 ? (T2) items[2] : default;
            t3 = items.Length > 3 ? (T3) items[3] : default;
            t4 = items.Length > 4 ? (T4) items[4] : default;
            t5 = items.Length > 5 ? (T5) items[5] : default;
            t6 = items.Length > 6 ? (T6) items[6] : default;
            t7 = items.Length > 7 ? (T7) items[7] : default;
        }
    }
}