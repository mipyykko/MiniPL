using System;
using System.Collections.Generic;
using System.Data;
using MiniPL.Common.Errors;

namespace MiniPL.Common
{
    public class Text
    {
        private IErrorService ErrorService => Context.ErrorService;
        private readonly string text;
        public int End { get; private set; }
        public List<string> Lines { get; private set; }

        private Text(string text)
        {
            this.text = text;
            End = text.Length;

            Lines = new List<string>(text.Split('\n'));
        }

        public static Text Of(string text)
        {
            return new Text(text);
        }

        public int Pos { get; private set; } = 0;
        public int Line { get; private set; } = 0;
        public int LinePos { get; private set; } = 0;
        public bool IsExhausted => Pos >= End;
        public char Current => Pos < End ? text[Pos] : '\0';
        public char Peek => Pos + 1 < End ? text[Pos + 1] : '\0';
        private string NextTwo => $"{Current}{Peek}";

        public string Range(int start, int len)
        {
            return text.Substring(start, len);
        }

        public void Advance(int n)
        {
            if (n < 0) throw new ArgumentException();
            var startPos = Pos;
            while (Pos < startPos + n && !IsExhausted)
            {
                if (Current == '\n')
                {
                    Line++;
                    LinePos = 0;
                }
                else
                {
                    LinePos++;
                }

                Pos++;
            }
        }

        public void Advance()
        {
            Advance(1);
        }

        public char Next()
        {
            var p = Peek;

            if (p != '\0') Advance();

            return p;
        }

        public void SkipSpacesAndComments()
        {
            SkipSpaces();
            var done = false;
            var startPos = Pos;
            var startLine = Line;
            var startLinePos = LinePos;

            while (!done && !IsExhausted)
            {
                done = true;
                switch (NextTwo)
                {
                    // line comment, skip this line and continue loop
                    case "//":
                        SkipLine();
                        done = false;
                        continue;
                    // block comment, advance until end marker or EOF
                    case "/*":
                    {
                        Advance(2);
                        while (!IsExhausted && NextTwo != "*/")
                        {
                            done = false;
                            Advance();
                            if (!IsExhausted) continue;

                            ErrorService.Add(
                                ErrorType.SyntaxError,
                                Token.Of(
                                    TokenType.Unknown,
                                    SourceInfo.Of(
                                        (startPos, Pos),
                                        (startLine, startLinePos, Pos
                                        ))),
                                "runaway comment",
                                true);
                        }

                        Advance(2);
                        break;
                    }
                }

                SkipSpaces();
            }
        }

        public void SkipSpaces()
        {
            var curr = Current;

            while ((curr == ' ' || curr == '\n') && !IsExhausted) curr = Next();
        }

        public void SkipLine()
        {
            while (Current != '\n' && !IsExhausted) Advance();
            Advance();
        }

        public static bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }
    }
}