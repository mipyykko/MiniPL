using System.Linq;
using Compiler.Common;

namespace Compiler.Parse
{
    public static class Grammar
    {
        public static readonly KeywordType[] DefaultStatementFirstKeywords =
        {
            KeywordType.Var,
            KeywordType.For,
            KeywordType.Read,
            KeywordType.Print,
            KeywordType.Assert
        };
        
        public static readonly KeywordType[] DoBlockStatementFirstKeywords =
            DefaultStatementFirstKeywords.Concat(new[] {KeywordType.End}).ToArray();

        public static readonly TokenType[] StatementFirstTokens = 
        {
            TokenType.Keyword,
            TokenType.Identifier
        };
        
        public static readonly TokenType[] ExpressionFirstTokens =
        {
            TokenType.IntValue,
            TokenType.StringValue,
            TokenType.BoolValue,
            TokenType.Identifier,
            TokenType.OpenParen,
            TokenType.Operator
        };
        
        public static readonly TokenType[] OperandFirstTokens =
        {
            TokenType.IntValue,
            TokenType.StringValue,
            TokenType.BoolValue,
            TokenType.Identifier,
            TokenType.OpenParen,
            TokenType.Operator
        };

        public static readonly string[] UnaryOperators =
        {
            "!",
            "-"
        };






    }
}