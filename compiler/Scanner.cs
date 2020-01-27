using System;
using System.Collections.Generic;
using System.Text;

namespace Compiler
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

        private SourceInfo GetSourceInfo => SourceInfo.Of((startPos, text.Pos), (text.Line, startLinePos, text.LinePos));

        public List<Token> Scan()
        {
            while (!text.IsExhausted)
            {
                text.SkipSpaces();
                char curr = text.Current;
                char next = text.Peek();

                if (text.IsExhausted)
                {
                    break;
                }

                TokenType tokenType = Token.GetTokenType(curr);
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
                    case TokenType.Division:
                        if (next == '/') { 
                            text.SkipLine();  // line comment
                        } else if (next == '*') { 
                            HandleBlockComment(); // block comment
                        }
                        else
                        {
                            goto default;
                        }
                        break;
                    case TokenType.Dot:
                        if (next == '.')
                        {
                            text.Advance();
                            AddToken(TokenType.Range);
                        }
                        else
                        {
                            goto default;
                        }
                        break;
                    case TokenType.Colon:
                        if (next == '=')
                        {
                            text.Advance();
                            AddToken(TokenType.Assignment);
                        }
                        else
                        {
                            goto default;
                        }
                        break;
                    case TokenType.Unknown:
                        if (char.IsLetter(curr))
                        {
                            string atom = GetAtom();
                            KeywordType kw = Token.GetKeywordType(atom);

                            if (kw != KeywordType.Unknown)
                            {
                                AddToken(TokenType.Keyword, kw, atom);
                            }
                            else
                            {
                                AddToken(TokenType.Identifier, atom);
                            }
                        }
                        break;
                    default:
                        AddToken(tokenType);
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
            tokens.Add(new Token(type, kw, contents, GetSourceInfo));
        }
        private void AddToken(TokenType type, string contents)
        {
            AddToken(type, KeywordType.Unknown, contents);
        }
        private void AddToken(TokenType type)
        {
            AddToken(type, "");
        }

        private string GetAtom()
        {
            StringBuilder kw = new StringBuilder("" + text.Current);
            char curr;

            while (!text.IsExhausted)
            {
                curr = text.Peek();
                if (" \t\n".IndexOf(curr) < 0 && char.IsLetter(curr))
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
            text.Advance(); // skip "
            char curr = text.Current;
            StringBuilder str = new StringBuilder($"{curr}");

            while (!text.IsExhausted && (curr = text.Peek()) != '"') 
            {
                str.Append(curr);
                text.Advance();
            }

            text.Advance();
            return str.ToString();
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
