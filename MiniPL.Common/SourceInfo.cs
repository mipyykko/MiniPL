using System;

namespace MiniPL.Common
{
    public class SourceInfo
    {
        public readonly (int Start, int End) SourceRange;

        /// <summary>
        /// The line range in a tuple: (line, start, end).
        /// </summary>
        public readonly (int Line, int Start, int End) LineRange;

        private SourceInfo((int, int) sourceRange, (int, int, int) lineRange)
        {
            SourceRange = sourceRange;
            LineRange = lineRange;
        }

        public static SourceInfo Of((int, int) sourceRange, (int, int, int) lineRange)
        {
            return new SourceInfo(sourceRange, lineRange);
        }
    }
}