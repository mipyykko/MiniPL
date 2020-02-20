using System;
namespace Compiler
{
    public class Range
    {
        public int Start { get; private set; } = 0;
        public int End { get; private set; } = 0;

        private Range(int start, int end)
        {
            Start = start;
            End = end;
        }

        public static Range Of(int start, int end)
        {
            return new Range(start, end);
        }

        public override string ToString()
        {
            return $"{Start}:{End}";
        }
    }
}
