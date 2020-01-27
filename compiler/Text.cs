using System;

namespace Compiler
{
    public class Text
    {
        private readonly string text;
        private int end;

        public Text(string text)
        {
            this.text = text;
            this.end = text.Length - 1;
        }

        public static Text Of(string text)
        {
            return new Text(text);
        }

        public int Pos { get; private set; } = 0;
        public int Line { get; private set; } = 0;
        public int LinePos { get; private set; } = 0;
        public bool IsExhausted => Pos > end;
        public char Current => text[Pos];

        public char Peek()
        {
            if (Pos < end)
            {
                return text[Pos + 1];
            }

            return '\0';
        }

        public void Advance(int n)
        {
            int startPos = Pos;
            while (Pos < startPos + n)
            {
                if (!IsExhausted)
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
            char p = Peek();

            if (p != '\0')
            {
                Advance();
            }

            return p;
        }

        public void SkipSpaces()
        {
            char curr = Current;

            while ((curr == ' ' || curr == '\n') && !IsExhausted)
            {
                if (curr == '\n') { 
                    Line++; 
                    LinePos = 0; 
                }
                curr = Next();
            }
        }

        public void SkipLine()
        {
            while (Peek() != '\n' && !IsExhausted) { Advance(); }
        }
    }
}
