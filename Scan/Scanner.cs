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

        // TODO/FIXME: does startPos & endPos +1 on TypicalTokens 
        // may need to do other way - depends on where advance called - includes " from string etc.
        private SourceInfo GetSourceInfo => SourceInfo.Of((startPos, text.Pos), (text.Line, startLinePos, text.LinePos));

        public List<Token> Scan()
        {
            while (!text.IsExhausted)
            {
                text.SkipSpacesAndComments();
                char curr = text.Current;
                char next = text.Peek();

                if (text.IsExhausted)
                {
                    break;
                }

                (TokenType tokenType, string token) = GetTrivialToken();
                startPos = text.Pos;
                startLinePos = text.LinePos;

                switch (tokenType)
                {
                    case TokenType.Number:
                        string numberContents = GetNumberContents();
                        AddToken(TokenType.IntValue, numberContents);
                        break;
                    case TokenType.Quote:
                        string stringContents = GetStringContents();
                        AddToken(TokenType.StringValue, stringContents);
                        break;
                    case TokenType.Unknown:
                        if (char.IsLetter(curr))
                        {
                            string atom = GetAtom();
                            KeywordType kw = Token.GetKeywordType(atom);

                            if (kw != KeywordType.Unknown)
                            {
                                AddToken(TokenType.Keyword, kw, atom);
                                break;
                            }
                            AddToken(TokenType.Identifier, atom);
                            break;
                        }
                        AddToken(TokenType.Unknown, curr);
                        break;
                    default:
                        AddToken(tokenType, token);
                        break;
                }
                text.Advance();
            }
            AddToken(TokenType.EOF); 

            foreach (Token tok in tokens)
            {
                Console.WriteLine(tok);
            }
            return tokens;
        }

        private void AddToken(TokenType type, KeywordType kw, string contents)
        {
            (int start, int end) = GetSourceInfo.sourceRange;
            Console.WriteLine("token contents {0}, source {1}", contents, text.Range(start, (end - start)));
            tokens.Add(new Token(type, kw, contents, GetSourceInfo));
        }
        private void AddToken(TokenType type, string contents)
        {
            AddToken(type, KeywordType.Unknown, contents);
        }
        private void AddToken(TokenType type, char content) 
        {
            AddToken(type, KeywordType.Unknown, "" + content);
        }
        private void AddToken(TokenType type)
        {
            AddToken(type, "");
        }

        private (TokenType, string) GetTrivialToken()
        {
            foreach (string token in Token.TrivialTokenTypes.Keys)
            {
                if (text.Pos + token.Length < text.End && text.Range(text.Pos, token.Length) == token)
                {
                    text.Advance(token.Length);
                    return (Token.TrivialTokenTypes[token], token);
                }
            }
            return (char.IsDigit(text.Current) ? TokenType.Number : TokenType.Unknown,
                    $"{text.Current}");
        }

        private string GetAtom()
        {
            StringBuilder kw = new StringBuilder($"{text.Current}");
            char curr;

            while (!text.IsExhausted)
            {
                curr = text.Peek();
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
            char curr = text.Current;
            StringBuilder n = new StringBuilder($"{curr}");

            while (!text.IsExhausted && char.IsDigit(curr = text.Peek()))
            {
                n.Append(curr);
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
                    text.Advance();
                    char peeked = text.Current;
                    switch (peeked)
                    {
                        case 'n':
                            str.Append("\n");
                            break;
                        case 't':
                            str.Append("\t");
                            break;
                        case '\\':
                            str.Append("\\");
                            Console.WriteLine("wehey! {0}", str);
                            break;
                        default:
                            if (char.IsDigit(peeked))
                            {
                                int numberValue = Int16.Parse(GetNumberContents());
                                str.Append(Convert.ToChar(numberValue));
                                break;
                            }
                            error = String.Format("unknown special character \\{0} at {1}", peeked, text.Pos);
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


        private void HandleBlockComment()
        {
            text.Advance(2);

            while (!text.IsExhausted) 
            {
                char curr = text.Next(); 
                if (curr == '*' && text.Peek() == '/')
                {
                    text.Advance();
                    return;
                }
            }
        }
    }
}
