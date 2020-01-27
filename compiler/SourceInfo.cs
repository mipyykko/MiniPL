using System;

namespace Compiler
{
    public class SourceInfo
    {
        public readonly (int, int) sourceRange;
        /// <summary>
        /// The line range in a tuple: (line, start, end).
        /// </summary>
        public readonly (int, int, int) lineRange;

        private SourceInfo((int, int) sourceRange, (int, int, int) lineRange)
        {
            this.sourceRange = sourceRange;
            this.lineRange = lineRange;
        }

        public static SourceInfo Of((int, int) sourceRange, (int, int, int) lineRange)
        {
            return new SourceInfo(sourceRange, lineRange);
        }
    }
}
