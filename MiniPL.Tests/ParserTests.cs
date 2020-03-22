using System;
using MiniPL.Common;
using MiniPL.Common.AST;
using MiniPL.Common.Errors;
using MiniPL.Parse;
using MiniPL.Scan;
using Moq;
using NUnit.Framework;

namespace MiniPL.Tests
{
    public class ParserTests
    {
        [TestCase("a := 1;", "Assignment", TokenType.Assignment, KeywordType.Unknown)]
        [TestCase("a := a + 1;", "Assignment", TokenType.Assignment, KeywordType.Unknown)]
        [TestCase("a := -1;", "Assignment", TokenType.Assignment, KeywordType.Unknown)]
        [TestCase("a := a * -1;", "Assignment", TokenType.Assignment, KeywordType.Unknown)]
        [TestCase("a := -a;", "Assignment", TokenType.Assignment, KeywordType.Unknown)]
        [TestCase("assert(1 < 2);", "Statement", TokenType.Keyword, KeywordType.Assert)]
        [TestCase("assert(!(1 < 2));", "Statement", TokenType.Keyword, KeywordType.Assert)]
        [TestCase("print a;", "Statement", TokenType.Keyword, KeywordType.Print)]
        [TestCase("read a;", "Statement", TokenType.Keyword, KeywordType.Read)]
        [TestCase("for i in 0..a do\nprint a;\nend for;", "For", TokenType.Keyword, KeywordType.For)]
        [TestCase("var a : string := 1;", "Assignment", TokenType.Keyword, KeywordType.Var)]
        public void StatementTest(string program, string nodeType, TokenType tokenType, KeywordType keywordType)
        {
            Context.Source = Text.Of(program);
            var scanner = new Scanner();
            var parser = new Parser(scanner);

            var tree = (StatementListNode) parser.Program();
            var left = tree.Left;
            Console.Write($"{program} {left.Name}");

            Assert.AreEqual(
                "StatementList",
                tree.Name
            );

            Assert.AreEqual(
                nodeType,
                left.Name
            );
            Assert.AreEqual(
                tokenType,
                left.Token.Type
            );
            Assert.AreEqual(
                keywordType,
                left.Token.KeywordType
            );
        }

        public class Unexpected
        {
            private Mock<IErrorService> errorService;

            [SetUp]
            public void Setup()
            {
                errorService = new Mock<IErrorService>();
                Context.ErrorService = errorService.Object;
            }

            [TearDown]
            public void TearDown()
            {
                Context.ErrorService = null;
            }

            private void Parse(string program)
            {
                Context.Source = Text.Of(program);
                var scanner = new Scanner();
                var parser = new Parser(scanner);

                parser.Program();
            }

            [TestCase("var i : in := 1;",
                "expected one of keyword types Int, String, Bool, got in of type In",
                Description = "multiple possible keywords")]
            [TestCase("for i in 0..2 do\nprint i;\nend fo",
                "expected keyword of type For, got fo of type Unknown",
                Description = "single possible keyword")]
            [TestCase("in 0..2 do\nprint a;\nend for;",
                "expected one of keyword types Var, For, Read, Print, Assert, got in of type In",
                Description = "multiple possible keywords in statement first position")]
            [TestCase("end for;",
                "expected one of keyword types Var, For, Read, Print, Assert, got end of type End",
                Description = "End keyword in illegal position")]
            [TestCase("for i in 0..2 do\nin i;\nend for;",
                "expected one of keyword types Var, For, Read, Print, Assert, End, got in of type In",
                Description = "illegal keyword in for block; end listed as possible")]
            public void KeywordTest(string program, string errorString)
            {
                Parse(program);

                errorService.Verify(es => es.Add(
                    It.Is<ErrorType>(et => et == ErrorType.UnexpectedKeyword),
                    It.IsAny<Token>(),
                    It.Is<string>(s => s.Contains(errorString)),
                    false
                ));
            }

            [TestCase("var i int := 1;",
                "expected token of type Colon, got int of type Keyword")]
            [TestCase("assert(1 < 2;",
                "expected token of type CloseParen, got ; of type Separator")]
            [TestCase("read;",
                "expected token of type Identifier, got ; of type Separator")]
            [TestCase("print n",
                "expected token of type Separator, got EOF of type EOF")]
            [TestCase("var i : int := *2;",
                "expected one of !, -, got *",
                ErrorType.SyntaxError)]
            [TestCase("assert (+(1 < 2));",
                "expected one of !, -, got +",
                ErrorType.SyntaxError)]
            [TestCase("a := +1",
                "expected one of !, -, got +",
                ErrorType.SyntaxError)]
            [TestCase(": i := 1;",
                "expected one of token types Keyword, Identifier, got : of type Colon")]
            public void TokenTest(string program, string errorString, ErrorType type = ErrorType.UnexpectedToken)
            {
                Parse(program);

                errorService.Verify(es => es.Add(
                    It.Is<ErrorType>(et => et == type),
                    It.IsAny<Token>(),
                    It.Is<string>(s => s.Contains(errorString)),
                    false
                ));
            }
        }
    }
}