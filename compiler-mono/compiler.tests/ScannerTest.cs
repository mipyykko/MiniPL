using FluentAssertions;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Compiler.Common;
using Compiler.Scan;
using Text = Compiler.Common.Text;

namespace Compiler.Tests.ScannerTests
{
    [TestFixture()]
    public class ScannerTests
    {
        private string TokenListToString(List<Token> l)
        {
            return string.Join("\r\n", l);
        }

        private void CompareTokenLists(List<Token> a, List<Token> b)
        {
            Assert.AreEqual(a.Count, b.Count);
            a.Select((t, idx) => t.ToString().Should().BeEquivalentTo(b[idx].ToString()));
        }

        private void AssertProgramTokens(string program, string expected)
        {
            Scanner s = new Scanner(Text.Of(program));
            List<Token> tokens = s.Scan();

            string tokenString = TokenListToString(tokens);
            tokenString.Should().BeEquivalentTo(expected);
        }

        [Test()]
        public void SimpleCase()
        {
            string program1 = "var a : int := 0;";
            string expected = 
$@"Keyword (0, 2) (0, 0, 2) Var ""var""
Identifier (4, 4) (0, 4, 4) Unknown ""a""
Colon (6, 6) (0, 6, 6) Unknown "":""
Keyword (8, 10) (0, 8, 10) Int ""int""
Assignment (12, 13) (0, 12, 13) Unknown "":=""
IntValue (15, 15) (0, 15, 15) Unknown ""0""
Separator (16, 16) (0, 16, 16) Unknown "";""
EOF (16, 17) (0, 16, 17) Unknown """"";

            AssertProgramTokens(program1, expected);
        }

        [Test()]
        public void Comments()
        {
            string program2 = "// print \"commented out\";\nprint \"not commented\";/* line change\n in comment\n*/\nprint \"a/* not comment */ // b\";";
            string expected =
$@"Keyword (26, 30) (2, 0, 4) Print ""print""
StringValue (32, 46) (2, 6, 20) Unknown ""not commented""
Separator (47, 47) (2, 21, 21) Unknown "";""
Keyword (78, 82) (6, 0, 4) Print ""print""
StringValue (84, 108) (6, 6, 30) Unknown ""a/* not comment */ // b""
Separator (109, 109) (6, 31, 31) Unknown "";""
EOF (109, 110) (6, 31, 32) Unknown """"";

            AssertProgramTokens(program2, expected);
        }
    }
}
