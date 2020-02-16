﻿using System;
using System.Collections.Generic;

namespace Compiler.Common
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
        Operator,
        Unary,
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
        public TokenType Type { get; private set; }
        public KeywordType KeywordType { get; private set; }
        public string Content { get; private set; }
        public SourceInfo SourceInfo {get; private set; }

        public Token(TokenType type, KeywordType kw, string content, SourceInfo sourceInfo)
        {
            Type = type;
            KeywordType = kw;
            Content = content;
            SourceInfo = sourceInfo;
        }

        public static Token Of(TokenType type, KeywordType kw, string content, SourceInfo sourceInfo)
        {
            return new Token(type, kw, content, sourceInfo);
        }
        public static Token Of(TokenType type, SourceInfo sourceInfo)
        {
            return Token.Of(type, KeywordType.Unknown, "", sourceInfo);
        }
        public static Token Of(TokenType type, string content, SourceInfo sourceInfo)
        {
            return Token.Of(type, KeywordType.Unknown, content, sourceInfo);
        }

        public override string ToString()
        {
            return $"{Type} {SourceInfo.sourceRange} {SourceInfo.lineRange} {KeywordType} \"{Content}\"";
        }

        public static Dictionary<string, TokenType> TrivialTokenTypes = new Dictionary<string, TokenType>()
        {
            // could be simplified to have "operator" and so on
            [":="] = TokenType.Assignment,
            [".."] = TokenType.Range,
            [";"] = TokenType.Separator,
            ["("] = TokenType.OpenParen,
            [")"] = TokenType.CloseParen,
            ["\""] = TokenType.Quote,
            ["."] = TokenType.Dot,
            [":"] = TokenType.Colon,
            ["="] = TokenType.Operator, // Equals
            ["<"] = TokenType.Operator, // LessThan
            ["+"] = TokenType.Operator, // Addition,
            ["-"] = TokenType.Operator, // Subtraction,
            ["*"] = TokenType.Operator, // Multiplication,
            ["/"] = TokenType.Operator, // Division,
            ["&"] = TokenType.Operator, // And,
            // unary? how extensible would we want to be
            ["!"] = TokenType.Not
        };

        private static Dictionary<string, KeywordType> StringToKeywordType = new Dictionary<string, KeywordType>()
        {
            ["var"] = KeywordType.Var,
            ["for"] = KeywordType.For,
            ["end"] = KeywordType.End,
            ["in"] = KeywordType.In,
            ["do"] = KeywordType.Do,
            ["read"] = KeywordType.Read,
            ["print"] = KeywordType.Print,
            ["int"] = KeywordType.Int,
            ["string"] = KeywordType.String,
            ["bool"] = KeywordType.Bool,
            ["assert"] = KeywordType.Assert
        };

        public static KeywordType GetKeywordType(string s)
        {
            KeywordType kwt = KeywordType.Unknown;
            StringToKeywordType.TryGetValue(s, out kwt);

            return kwt;
        }
    }
}
