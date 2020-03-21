using System;
using FluentAssertions;
using NUnit.Framework;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Compiler.Common;
using Compiler.Common.Errors;
using Compiler.Scan;
using Text = Compiler.Common.Text;
using Moq;

namespace Compiler.Tests
{
    [ExcludeFromCodeCoverage]
    [TestFixture()]
    public class ScannerTests
    {
        private string TokenListToString(List<Token> l)
        {
            return string.Join("\r\n", l);
        }

        private static List<Token> Scan(string program)
        {
            Context.Source = Text.Of(program);
            var s = new Scanner();

            var tokens = new List<Token>();
            Token token = null;

            while (token == null || token.Type != TokenType.EOF)
            {
                token = s.GetNextToken();
                tokens.Add(token);
            }

            return tokens;
        }

        private void AssertProgramTokens(string program, string expected)
        {
            var tokens = Scan(program);
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
EOF (29, 29) (5, 10, 10) Unknown ""EOF""";

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
EOF (125, 125) (4, 48, 48) Unknown ""EOF""";

            AssertProgramTokens(program2, expected);
        }

        [Test()]
        public void UnterminatedString()
        {
            var program = "print \"this string is not terminated";

            var errorMock = new Mock<IErrorService>();
            Context.ErrorService = errorMock.Object;

            Scan(program);

            errorMock.Verify(es => es.Add(
                It.Is<ErrorType>(e => e == ErrorType.UnterminatedStringTerminal),
                It.Is<Token>(token => 
                    token.SourceInfo.SourceRange.Start == 6 &&
                    token.SourceInfo.SourceRange.End == 6
                ),
                It.IsAny<string>(),
                true
            ), Times.Once());
        }

        [TestFixture]
        public class StringLiterals
        {
            Mock<IErrorService> errorMock; 
            [SetUp]
            public void Setup()
            {
                errorMock = new Mock<IErrorService>();
                Context.ErrorService = errorMock.Object;
            }

            [TearDown]
            public void Teardown()
            {
                Context.ErrorService = null;
            }

            [Test]
            public void Valid()
            {
                var program = $@"""kissa\t\t\\koira\n\n\""\32\64\255\0""";
                var tokens = Scan(program);

                Assert.AreEqual(
                    "kissa\t\t\\koira\n\n\"\x20\x40\xFF\x0", 
                    tokens[0].Content);
            }

            [TestCase("\"\\99999\"", ErrorType.SyntaxError, "unknown special character \\99999")]
            [TestCase("\"\\k\"", ErrorType.SyntaxError, "unknown special character \\k")]
            public void Illegal(string str, ErrorType err, string message)
            {
                Scan(str);

                errorMock.Verify(es => es.Add(
                    It.Is<ErrorType>(e => e == err),
                    It.IsAny<Token>(),
                    It.Is<string>(s => s.Equals(message)),
                    false
                ));
            }
        }
    }
}