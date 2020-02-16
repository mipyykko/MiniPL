using System;
using System.Collections.Generic;
using System.Text;
using Compiler.Common;

namespace Compiler.Scan
{
    public class Scanner
    {
        private Text text;
        private List<Token> tokens = new List<Token>();
        private int startPos;
        private int startLinePos;

        public Scanner(Text text)
        {
            this.text = text;
        }

        private SourceInfo GetSourceInfo(string token) {
            int tokenLength = Math.Max(0, token.Length - 1);
            return SourceInfo.Of(
                (startPos, startPos + tokenLength), 
                (text.Line, startLinePos, startLinePos + tokenLength)
            );
        }

        public Token GetNextToken()
        {
            text.SkipSpacesAndComments();
            char curr = text.Current;
            char next = text.Peek;

            if (text.IsExhausted)
            {
                return Token.Of(TokenType.EOF, GetSourceInfo(""));
            }

            startPos = text.Pos;
            startLinePos = text.LinePos;
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
            string curr = $"{text.Current}";

            if (Text.IsDigit(text.Current)) {
                return (TokenType.Number, curr);
            }
            if (char.IsLetter(text.Current)) // we don't have any trivial tokens starting with letters now
            {
                return (TokenType.Unknown, curr);
            }

            foreach (string token in Token.TrivialTokenTypes.Keys)
            {
                if (text.Pos + token.Length <= text.End && text.Range(text.Pos, token.Length).Equals(token))
                {
                    text.Advance(token.Length);
                    return (Token.TrivialTokenTypes[token], token);
                }
            }
            return (TokenType.Unknown, curr);
        }

        private string GetAtom()
        {
            StringBuilder kw = new StringBuilder("");
            char curr;

            while (!text.IsExhausted)
            {
                curr = text.Current;
                if (" \t\n".IndexOf(curr) < 0 && (char.IsLetter(curr) || char.IsDigit(curr) || curr == '_'))
                {
                    kw.Append(curr);
                    text.Advance();
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

            while (!text.IsExhausted && char.IsDigit(text.Current))
            {
                n.Append(text.Current);
                text.Advance();
            }

            return n.ToString();
        }

        private string GetStringContents()
        {
            // text.Advance(); // skip "
            char curr = text.Current;
            StringBuilder str = new StringBuilder();

            string error = null;

            while (!text.IsExhausted) 
            {
                curr = text.Current;

                if (curr == '"')
                {
                    // TODO: what to do with error?
                    text.Advance();
                    return str.ToString();
                }
                if (curr == '\\')
                {
                    switch (text.Peek)
                    {
                        case 'n':
                            str.Append("\n");
                            text.Advance();
                            break;
                        case 't':
                            str.Append("\t");
                            text.Advance();
                            break;
                        case '\\':
                            str.Append("\\");
                            text.Advance();
                            break;
                        default:
                            if (Text.IsDigit(text.Peek))
                            {
                                int numberValue = Int16.Parse(GetNumberContents());
                                str.Append(Convert.ToChar(numberValue));
                                break;
                            }
                            error = String.Format("unknown special character \\{0} at {1}", text.Peek, text.Pos);
                            break;
                    }
                    text.Advance();
                }
                else
                {
                    str.Append(curr);
                    text.Advance();
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
