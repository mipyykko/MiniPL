using System.Linq;
using MiniPL.Common;

namespace MiniPL.Parse
{
    public static class Grammar
    {
        private static readonly KeywordType[] DefaultStatementFirstKeywords =
        {
            KeywordType.Var,
            KeywordType.For,
            KeywordType.Read,
            KeywordType.Print,
            KeywordType.Assert
        };
        
        private static readonly KeywordType[] DoBlockStatementFirstKeywords =
            DefaultStatementFirstKeywords.Concat(new[] {KeywordType.End}).ToArray();

        public static readonly TokenType[] StatementFirstTokens = 
        {
            TokenType.Keyword,
            TokenType.Identifier
        };
        
        public static KeywordType[] StatementFirstKeywords(bool isDoBlock = false) =>
            isDoBlock
                ? DoBlockStatementFirstKeywords
                : DefaultStatementFirstKeywords;


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
            "!", "-"
        };

        public static readonly string[] BinaryOperators =
        {
            "+", "-", "*", "/", "&", "<", "="
        };

        public static readonly KeywordType[] ExpectedTypes =
        {
            KeywordType.Int,
            KeywordType.String,
            KeywordType.Bool
        };







    }
}