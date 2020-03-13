using System;
using FluentAssertions;
using NUnit.Framework;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Compiler.Common;
using Compiler.Scan;
using Text = Compiler.Common.Text;

namespace Compiler.Tests
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
            var s = new Scanner(Text.Of(program));

            var tokens = new List<Token>();
            Token token = null;

            while (token == null || token.Type != TokenType.EOF)
            {
                token = s.GetNextToken();
                tokens.Add(token);
            }

            var tokenString = TokenListToString(tokens);
            tokenString.Should().BeEquivalentTo(expected);
        }

        [Test()]
        public void Whitespace()
        {
            var program1 = "var a        :\n\n\n\n\nint    :=0;";
            var expected =
                $@"Keyword (0, 2) (0, 0, 2) Var ""var""
Identifier (4, 4) (0, 4, 4) Unknown ""a""
Colon (13, 13) (0, 13, 13) Unknown "":""
Keyword (19, 21) (5, 0, 2) Int ""int""
Assignment (26, 27) (5, 7, 8) Unknown "":=""
IntValue (28, 28) (5, 9, 9) Unknown ""0""
Separator (29, 29) (5, 10, 10) Unknown "";""
EOF (29, 29) (5, 10, 10) Unknown """"";

            AssertProgramTokens(program1, expected);
        }

        [Test()]
        public void Comments()
        {
            var program2 =
                "// print \"commented out\";\n" + 
                "print \"not commented\";/* line change\n"+
                "in comment\n"+
                "*/\n"+
                "print \"a/* not a comment inside literal */ // b\";";
            var expected =
                $@"Keyword (26, 30) (1, 0, 4) Print ""print""
StringValue (32, 44) (1, 6, 18) Unknown ""not commented""
Separator (47, 47) (1, 21, 21) Unknown "";""
Keyword (77, 81) (4, 0, 4) Print ""print""
StringValue (83, 122) (4, 6, 45) Unknown ""a/* not a comment inside literal */ // b""
Separator (125, 125) (4, 48, 48) Unknown "";""
EOF (125, 125) (4, 48, 48) Unknown """"";

            AssertProgramTokens(program2, expected);
        }
    }
}