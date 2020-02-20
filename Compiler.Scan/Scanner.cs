using System;
using System.Collections.Generic;
using System.Text;
using Compiler.Common;

namespace Compiler.Scan
{
    public class Scanner
    {
        public Text Text { get; private set; }
        private int startPos;
        private int startLinePos;

        private char Current => Text.Current;
        private char Peek => Text.Peek;
        private string NextTwo => Text.NextTwo;
        private bool IsExhausted => Text.IsExhausted;
        
        public Scanner(Text text)
        {
            Text = text;
        }

        private (int Start, int End) TokenRange(string token)
        {
            return (startPos, startPos + Math.Max(0, token.Length - 1));
        }

        private (int Line, int Start, int End) TokenLineRange(string token)
        {
            return (Text.Line, startLinePos, startLinePos + Math.Max(0, token.Length - 1));
        }

        private SourceInfo GetSourceInfo(string token)
        {
            var tokenLength = Math.Max(0, token.Length - 1);
            return SourceInfo.Of(
                TokenRange(token),
                TokenLineRange(token)
            );
        }

        public Token GetNextToken()
        {
            Text.SkipSpacesAndComments();

            if (IsExhausted) return Token.Of(TokenType.EOF, GetSourceInfo(""));

            startPos = Text.Pos;
            startLinePos = Text.LinePos;
            var (tokenType, token) = GetTrivialToken();

            switch (tokenType)
            {
                case TokenType.Number:
                    var numberContents = GetNumberContents(); // TODO: error check
                    return Token.Of(TokenType.IntValue, numberContents, GetSourceInfo(numberContents));
                case TokenType.Quote:
                    var stringContents = GetStringContents(); // TODO: error check
                    return Token.Of(TokenType.StringValue, stringContents, GetSourceInfo(stringContents));
                case TokenType.Unknown when char.IsLetter(Current):
                    var atom = GetAtom();
                    var kw = Token.GetKeywordType(atom.ToLower());

                    if (kw != KeywordType.Unknown) return Token.Of(TokenType.Keyword, kw, atom, GetSourceInfo(atom));
                    return Token.Of(TokenType.Identifier, atom, GetSourceInfo(atom));
                case TokenType.Unknown:
                    return Token.Of(TokenType.Unknown, $"{Current}", GetSourceInfo($"{Current}")); // TODO: error check?
                default:
                    return Token.Of(tokenType, token, GetSourceInfo(token));
            }
        }

        private (TokenType, string) GetTrivialToken()
        {
            if (Text.IsDigit(Current)) return (TokenType.Number, $"{Current}");
            if (char.IsLetter(Current)) // we don't have any trivial tokens starting with letters now
                return (TokenType.Unknown, $"{Current}");

            foreach (var token in Token.TrivialTokenTypes.Keys)
                if (Text.Pos + token.Length <= Text.End &&
                    Text.Range(Text.Pos, token.Length).ToLower().Equals(token.ToLower()))
                {
                    Text.Advance(token.Length);
                    return (Token.TrivialTokenTypes[token], token);
                }

            return (TokenType.Unknown, $"{Current}");
        }

        private string GetAtom()
        {
            var kw = new StringBuilder("");

            while (!Text.IsExhausted && " \t\n".IndexOf(Current) < 0 && (char.IsLetter(Current) || 
                                                                         char.IsDigit(Current) || Current == '_'))
            {
                kw.Append(Current);
                Text.Advance();
            }

            return kw.ToString();
        }

        private string GetNumberContents()
        {
            var n = new StringBuilder("");

            while (!Text.IsExhausted && Text.IsDigit(Current))
            {
                n.Append(Current);
                Text.Advance();
            }

            return n.ToString();
        }

        private Dictionary<char, string> Literals = new Dictionary<char,string>()
        {
            ['n'] = "\n",
            ['t'] = "\t",
            ['\\'] = "\\"
        };
        
        private string GetStringContents()
        {
            var str = new StringBuilder();

            string error = null;

            while (!Text.IsExhausted)
            {
                var peekedLiteral = Literals.TryGetValueOrDefault(Peek);

                switch (Current)
                {
                    case '"':
                        // TODO: what to do with error?
                        Text.Advance();
                        return str.ToString();
                    case '\\' when peekedLiteral == null && Text.IsDigit(Peek):
                        int numberValue = short.Parse(GetNumberContents());
                        str.Append(Convert.ToChar(numberValue));
                        Text.Advance();
                        break;
                    case '\\' when peekedLiteral == null:
                        error = $"unknown special character \\{Peek} at {Text.Pos}";
                        Text.Advance();
                        break;
                    case '\\':
                        str.Append(peekedLiteral);
                        Text.Advance(2);
                        break;
                    default:
                        str.Append(Current);
                        Text.Advance();
                        break;
                }
            }

            throw new Exception("unterminated string terminal");
            // if (error != null)
            // {
            //    // or add to error list
            //    throw new Exception(String.Format("Error: {0} in {1}", error, str));
            //}
            //text.Advance();
            //return str.ToString();
        }
    }
}