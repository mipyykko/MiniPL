using System;
using System.Collections.Generic;
using System.Text;
using Compiler.Common;

namespace Compiler.Scan
{
    public class Scanner
    {
        public Text Text { get; private set; }
        private List<Token> tokens = new List<Token>();
        private int startPos;
        private int startLinePos;

        public Scanner(Text text)
        {
            Text = text;
        }

        private SourceInfo GetSourceInfo(string token) {
            int tokenLength = Math.Max(0, token.Length - 1);
            return SourceInfo.Of(
                (startPos, startPos + tokenLength), 
                (Text.Line, startLinePos, startLinePos + tokenLength)
            );
        }

        public Token GetNextToken()
        {
            Text.SkipSpacesAndComments();
            char curr = Text.Current;
            char next = Text.Peek;

            if (Text.IsExhausted)
            {
                return Token.Of(TokenType.EOF, GetSourceInfo(""));
            }

            startPos = Text.Pos;
            startLinePos = Text.LinePos;
            (TokenType tokenType, string token) = GetTrivialToken();

            switch (tokenType)
            {
                case TokenType.Number:
                    string numberContents = GetNumberContents(); // TODO: error check
                    return Token.Of(TokenType.IntValue, numberContents, GetSourceInfo(numberContents));
                case TokenType.Quote:
                    string stringContents = GetStringContents(); // TODO: error check
                    return Token.Of(TokenType.StringValue, stringContents, GetSourceInfo(stringContents));
                case TokenType.Unknown:
                    if (char.IsLetter(curr))
                    {
                        string atom = GetAtom();
                        KeywordType kw = Token.GetKeywordType(atom);

                        if (kw != KeywordType.Unknown)
                        {
                            return Token.Of(TokenType.Keyword, kw, atom, GetSourceInfo(atom));
                        }
                        return Token.Of(TokenType.Identifier, atom, GetSourceInfo(atom));
                    }
                    return Token.Of(TokenType.Unknown, $"{curr}", GetSourceInfo($"{curr}")); // TODO: error check?
                default:
                    return Token.Of(tokenType, token, GetSourceInfo(token));
            }

        }
        public List<Token> Scan()
        {
            Token t;
            while ((t = GetNextToken()).Type != TokenType.EOF)
            {
                Console.WriteLine(t);
                tokens.Add(t);
            }

            return tokens;
        }

        private (TokenType, string) GetTrivialToken()
        {
            string curr = $"{Text.Current}";

            if (Text.IsDigit(Text.Current)) {
                return (TokenType.Number, curr);
            }
            if (char.IsLetter(Text.Current)) // we don't have any trivial tokens starting with letters now
            {
                return (TokenType.Unknown, curr);
            }

            foreach (string token in Token.TrivialTokenTypes.Keys)
            {
                if (Text.Pos + token.Length <= Text.End && Text.Range(Text.Pos, token.Length).Equals(token))
                {
                    Text.Advance(token.Length);
                    return (Token.TrivialTokenTypes[token], token);
                }
            }
            return (TokenType.Unknown, curr);
        }

        private string GetAtom()
        {
            StringBuilder kw = new StringBuilder("");
            char curr;

            while (!Text.IsExhausted)
            {
                curr = Text.Current;
                if (" \t\n".IndexOf(curr) < 0 && (char.IsLetter(curr) || char.IsDigit(curr) || curr == '_'))
                {
                    kw.Append(curr);
                    Text.Advance();
                }
                else
                {
                    break;
                }
            }

            return kw.ToString();
        }

        private string GetNumberContents()
        {
            StringBuilder n = new StringBuilder("");

            while (!Text.IsExhausted && char.IsDigit(Text.Current))
            {
                n.Append(Text.Current);
                Text.Advance();
            }

            return n.ToString();
        }

        private string GetStringContents()
        {
            char curr = Text.Current;
            StringBuilder str = new StringBuilder();

            string error = null;

            while (!Text.IsExhausted) 
            {
                curr = Text.Current;

                if (curr == '"')
                {
                    // TODO: what to do with error?
                    Text.Advance();
                    return str.ToString();
                }
                if (curr == '\\')
                {
                    switch (Text.Peek)
                    {
                        case 'n':
                            str.Append("\n");
                            Text.Advance();
                            break;
                        case 't':
                            str.Append("\t");
                            Text.Advance();
                            break;
                        case '\\':
                            str.Append("\\");
                            Text.Advance();
                            break;
                        default:
                            if (Text.IsDigit(Text.Peek))
                            {
                                int numberValue = Int16.Parse(GetNumberContents());
                                str.Append(Convert.ToChar(numberValue));
                                break;
                            }
                            error = String.Format("unknown special character \\{0} at {1}", Text.Peek, Text.Pos);
                            break;
                    }
                    Text.Advance();
                }
                else
                {
                    str.Append(curr);
                    Text.Advance();
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
