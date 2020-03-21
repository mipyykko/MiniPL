using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using MiniPL.Common;
using MiniPL.Common.Errors;
using NUnit.Framework;
using FluentAssertions;
using Text = MiniPL.Common.Text;
using Moq;

namespace MiniPL.Tests
{
    [ExcludeFromCodeCoverage]
    [TestFixture()]
    public class TextTest
    {
        [Test()]
        public void PosAdvanceTest()
        {
            var text = Text.Of("abc\nd");
            text.Pos.Should().Equals(0);
            text.Advance();
            text.Pos.Should().Equals(1);
            text.Advance(3);
            text.Pos.Should().Equals(4);
            text.Advance();
            text.Pos.Should().Equals(5);
            text.Advance();
            text.Pos.Should().Equals(5);
            text.IsExhausted.Should().BeTrue();

            Action throwingAct = () => text.Advance(-1);
            throwingAct.Should().Throw<ArgumentException>();

            text.IsExhausted.Should().BeTrue();
        }

        [Test()]
        public void LinePosAdvanceTest()
        {
            var text = Text.Of("abc\nd");
            text.Line.Should().Equals(0);
            text.LinePos.Should().Equals(0);
            text.Advance(2);
            text.Line.Should().Equals(0);
            text.LinePos.Should().Equals(2);
            text.Advance();
            text.Line.Should().Equals(1);
            text.LinePos.Should().Equals(0);
            text.Advance();
            text.Line.Should().Equals(1);
            text.LinePos.Should().Equals(0);
            text.Advance();
            text.IsExhausted.Should().BeTrue();
        }

        [Test()]
        public void NextPeekTest()
        {
            var text = Text.Of("ab\ncd");
            text.Peek.Should().Equals('b');
            text.Next();
            text.Peek.Should().Equals('\n');
            text.Next();
            text.Peek.Should().Equals('c');
            text.Next();
            text.Peek.Should().Equals('d');
            text.Next();
            text.Peek.Should().Equals('\0');
            text.Next();
            text.Peek.Should().Equals('\0');
            text.Advance();
            text.IsExhausted.Should().BeTrue();
        }

        [Test()]
        public void SkipSpacesAndCommentsTest()
        {
            var text = Text.Of($@"// skipping
not     skipping
/* should skip /* should skip
*/ back
");
            text.Current.Should().Equals('/');
            text.SkipSpacesAndComments();
            text.Current.Should().Equals('n');
            text.Advance(3);
            text.SkipSpacesAndComments();
            text.Current.Should().Equals('s');
            text.SkipLine();
            text.Current.Should().Equals('/');
            text.SkipSpacesAndComments();
            text.Current.Should().Equals('b');
        }

        [Test()]
        public void RunawayCommentTest()
        {
            var errorService = new Mock<IErrorService>();
            Context.ErrorService = errorService.Object;
            
            var text = Text.Of("/* asdf\nhas no end");
            
            text.SkipSpacesAndComments();

            errorService.Verify(es => es.Add(
                It.Is<ErrorType>(e => e == ErrorType.SyntaxError),
                It.IsAny<Token>(),
                It.IsAny<string>(),
                true), Times.Once
            );
        }

        [Test()]
        public void SkipLineTest()
        {
            var text = Text.Of("a\nb\nc");
            text.Current.Should().Equals('a');
            text.Advance();
            text.SkipLine();
            text.SkipLine();
            text.Current.Should().Equals('c');
            text.SkipLine();
            text.IsExhausted.Should().BeTrue();
        }

        [Test()]
        public void IsDigitTest()
        {
            Text.IsDigit('a').Should().BeFalse();
            Text.IsDigit('0').Should().BeTrue();
            Text.IsDigit('9').Should().BeTrue();
            Text.IsDigit(' ').Should().BeFalse();
        }
    }
}