using System;
using System.Collections.Generic;

namespace Compiler
{
    public enum TokenType
    {
        Unknown,
        EOF,
        Separator,
        OpenParen,
        CloseParen,
        Quote,
        Assignment,
        Dot,
        Colon,
        Range,
        Equals,
        LessThan,
        Addition,
        Subtraction,
        Multiplication,
        Division,
        And,
        Not,
        Keyword,
        Identifier,
        Number,
        StringValue,
        IntValue,
        BoolValue
    }

    public enum KeywordType
    {
        Unknown,
        Var,
        For,
        End,
        In,
        Do,
        Read,
        Print,
        Int,
        String,
        Bool,
        Assert
    }

    public class Token
    {
        private readonly TokenType type;
        private readonly KeywordType kw;
        private readonly string content;
        private readonly SourceInfo sourceInfo;

        public Token(TokenType type, KeywordType kw, string content, SourceInfo sourceInfo)
        {
            this.type = type;
            this.kw = kw;
            this.content = content;
            this.sourceInfo = sourceInfo;
        }

        public Token(TokenType type, string content, SourceInfo sourceInfo) : this(type, KeywordType.Unknown, content, sourceInfo) { }
        public Token(TokenType type, char c, SourceInfo sourceInfo) : this(type, KeywordType.Unknown, $"{c}", sourceInfo) { }
        public Token(TokenType type, KeywordType kw, SourceInfo sourceInfo) : this(type, kw, "", sourceInfo) { }
        public Token(TokenType type, SourceInfo sourceInfo) : this(type, KeywordType.Unknown, "", sourceInfo) { }

        public static Token Of(TokenType type, KeywordType kw, string content, SourceInfo sourceInfo)
        {
            return new Token(type, kw, content, sourceInfo);
        }

        public override string ToString()
        {
            return $"{type} {sourceInfo.sourceRange} {sourceInfo.lineRange} {kw} \"{content}\"";
        }

        private static Dictionary<char, TokenType> CharToTokenType = new Dictionary<char, TokenType>()
        {
            [';'] = TokenType.Separator,
            ['('] = TokenType.OpenParen,
            [')'] = TokenType.CloseParen,
            ['"'] = TokenType.Quote,
            ['.'] = TokenType.Dot,
            [':'] = TokenType.Colon,
            ['='] = TokenType.Equals,
            ['<'] = TokenType.LessThan,
            ['+'] = TokenType.Addition,
            ['-'] = TokenType.Subtraction,
            ['*'] = TokenType.Multiplication,
            ['/'] = TokenType.Division,
            ['&'] = TokenType.And,
            ['!'] = TokenType.Not,
            ['0'] = TokenType.Number,
            ['1'] = TokenType.Number,
            ['2'] = TokenType.Number,
            ['3'] = TokenType.Number,
            ['4'] = TokenType.Number,
            ['5'] = TokenType.Number,
            ['6'] = TokenType.Number,
            ['7'] = TokenType.Number,
            ['8'] = TokenType.Number,
            ['9'] = TokenType.Number
        };

        private static Dictionary<string, KeywordType> StringToKeywordType = new Dictionary<string, KeywordType>()
        {
            ["var"] = KeywordType.Var,
            ["for"] = KeywordType.For,
            ["in"] = KeywordType.In,
            ["do"] = KeywordType.Do,
            ["read"] = KeywordType.Read,
            ["print"] = KeywordType.Print,
            ["int"] = KeywordType.Int,
            ["string"] = KeywordType.String,
            ["bool"] = KeywordType.Bool,
            ["assert"] = KeywordType.Assert
        };

        public static TokenType GetTokenType(char c)
        {
            TokenType t = TokenType.Unknown;
            CharToTokenType.TryGetValue(c, out t);

            return t;
        }

        public static KeywordType GetKeywordType(string s)
        {
            KeywordType kwt = KeywordType.Unknown;
            StringToKeywordType.TryGetValue(s, out kwt);

            return kwt;
        }
    }
}
