using System;
using System.Collections.Generic;
using System.IO;
using Compiler.Common;
using Compiler.Common.AST;
using Compiler.Common.Errors;
using Compiler.Interpret;
using Moq;
using NUnit.Framework;

namespace Compiler.Tests
{
    public class ProgramVisitorTests
    {
        private Mock<IErrorService> errorServiceMock;
        private Mock<ISymbolTable> symbolTableMock;
        private ProgramVisitor visitor;
        private Mock<IProgramMemory> memoryMock;
        
        [SetUp]
        public void Setup()
        {
            errorServiceMock = new Mock<IErrorService>();
            symbolTableMock = new Mock<ISymbolTable>();
            Context.ErrorService = errorServiceMock.Object;
            Context.SymbolTable = symbolTableMock.Object;

            memoryMock = new Mock<IProgramMemory>();
            
            visitor = new ProgramVisitor(memoryMock.Object);
        }
        
        [TearDown]
        public void TearDown()
        {
            var sin = new StreamReader(Console.OpenStandardInput());
            Console.SetIn(sin); 
            
            var sout = new StreamWriter(Console.OpenStandardOutput())
            {
                AutoFlush = true
            };
            Console.SetOut(sout);
        }

        public StatementNode CreateStatementNode(KeywordType kwt, Node node)
        {
            return new StatementNode
            {
                Token = Token.Of(
                    TokenType.Keyword,
                    kwt,
                    "",
                    SourceInfo.Of((0, 0), (0, 0, 0))
                ),
                Arguments = new List<Node> {node}
            };
        }
        
        [TestCase("kissa")]
        public void StatementNodePrintTests(string ret)
        {
            var mockNode = new Mock<LiteralNode>();
            mockNode.Setup(n => n.Accept(visitor)).Returns(ret);
            
            var node = CreateStatementNode(KeywordType.Print, mockNode.Object);
            var output = new StringWriter();
            Console.SetOut(output);

            visitor.Visit(node);
            
            mockNode.Verify(n => n.Accept(visitor), Times.Once);
            Assert.AreEqual(ret, output.ToString());
        }

        [TestCase(true)]
        [TestCase(false)]
        public void StatementNodeAssertTests(bool ret)
        {
            var rep = $"{ret}";
            var mockNode = new Mock<ExpressionNode>();
            mockNode.Setup(n => n.Accept(visitor)).Returns(ret);
            mockNode.Setup(n => n.Representation()).Returns(rep);

            var node = CreateStatementNode(KeywordType.Assert, mockNode.Object);

            visitor.Visit(node);
            
            mockNode.Verify(n => n.Accept(visitor), Times.Once);

            if (!ret)
            {
                errorServiceMock.Verify(e => e.Add(
                    ErrorType.AssertionError,
                    node.Token,
                    $"assertion failed: {rep}",
                    true), 
                    Times.Once
                );
            }
        }

        [TestCase("a", "kissa", true)]
        [TestCase("a", "6")]
        public void StatementNodeReadTests(string id, string input, bool error = false)
        {
            var mockNode = new Mock<VariableNode>();
            var argumentToken = Token.Of(
                TokenType.Identifier,
                KeywordType.Unknown,
                id,
                SourceInfo.Of((0, 0), (0, 0, 0))
            );
            mockNode.Setup(n => n.Token).Returns(argumentToken);
            var node = CreateStatementNode(KeywordType.Read, mockNode.Object);
            
            memoryMock.Setup(m => m.UpdateVariable(id, input, false)).Returns(error ? ErrorType.TypeError : ErrorType.Unknown);
            symbolTableMock.Setup(s => s.LookupSymbol(id)).Returns(PrimitiveType.Int);

            var inputStream = new StringReader($"{input}\n");
            Console.SetIn(inputStream);
            
            visitor.Visit(node);

            memoryMock.Verify(m => m.UpdateVariable(id, input, false), Times.Once);

            if (error)
            {
                errorServiceMock.Verify(e => e.Add(
                    ErrorType.TypeError,
                    argumentToken,
                    It.IsAny<string>(),
                    true
                ));
            }
        }

        [TestCase(0, 5)]
        [TestCase(10, 0)]
        public void ForNodeTests(int start, int end)
        {
            var startNodeMock = new Mock<LiteralNode>();
            var endNodeMock = new Mock<LiteralNode>();
            var statementMock = new Mock<StatementNode>();

            startNodeMock.Setup(n => n.Accept(visitor)).Returns(start);
            endNodeMock.Setup(n => n.Accept(visitor)).Returns(end);

            var node = new ForNode
            {
                Id = new VariableNode
                {
                    Token = Token.Of(
                        TokenType.Identifier,
                        KeywordType.Unknown,
                        "a",
                        SourceInfo.Of((0, 0), (0, 0, 0))
                    )
                },
                RangeStart = startNodeMock.Object,
                RangeEnd = endNodeMock.Object,
                Statements = statementMock.Object
            };

            visitor.Visit(node);

            var times = Math.Abs(end - start) + 1;
            statementMock.Verify(s => s.Accept(visitor), Times.Exactly(times));
            memoryMock.Verify(m => m.UpdateControlVariable("a", It.IsAny<int>()), Times.Exactly(times + 1));

            var direction = end > start ? 1 : -1;
            var i = start;

            while (i != (direction > 0 ? end + 1 : start - 1))
            {
                memoryMock.Verify(m => m.UpdateControlVariable("a", i), Times.Once);
                i += direction;
            }
        }
    }
}