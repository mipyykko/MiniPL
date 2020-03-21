using System;
using System.Collections.Generic;
using System.IO;
using Compiler.Common;
using Compiler.Common.AST;
using Compiler.Common.Errors;
using Compiler.Interpret;
using Moq;
using NUnit.Framework;
using static Compiler.Tests.TestUtil;

namespace Compiler.Tests
{
    public class ProgramVisitorTests
    {
        private Mock<IErrorService> errorServiceMock;
        private Mock<ISymbolTable> symbolTableMock;
        private ISymbolTable symbolTable;
        private ProgramVisitor visitor;
        private Mock<IProgramMemory> memoryMock;
        private IProgramMemory memory;
        
        [SetUp]
        public void Setup()
        {
            errorServiceMock = new Mock<IErrorService>();
            symbolTableMock = new Mock<ISymbolTable>
            {
                CallBase = true
            };
            symbolTable = symbolTableMock.Object;
            symbolTable.DeclareSymbol("a", PrimitiveType.Int);
            symbolTable.DeclareSymbol("b", PrimitiveType.String);
            symbolTable.DeclareSymbol("c", PrimitiveType.Bool);

            Context.ErrorService = errorServiceMock.Object;
            Context.SymbolTable = symbolTable;

            memoryMock = new Mock<IProgramMemory>
            {
                CallBase = true
            };
            memory = memoryMock.Object;
            memory.UpdateVariable("a", "6");
            memory.UpdateVariable("b", "koira");
            memory.UpdateVariable("c", "true");

            visitor = new ProgramVisitor(memory);
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
                    MockSourceInfo
                ),
                Arguments = new List<Node> {node}
            };
        }
        
        [TestCase("kissa")]
        [TestCase(6)]
        public void StatementNodePrintTests(dynamic ret)
        {
            var mockNode = new Mock<LiteralNode>();
            mockNode.Setup(n => n.Accept(visitor)).Returns(ret);
            
            var node = CreateStatementNode(KeywordType.Print, mockNode.Object);
            var output = new StringWriter();
            Console.SetOut(output);

            visitor.Visit(node);
            
            mockNode.Verify(n => n.Accept(visitor), Times.Once);
            Assert.AreEqual(ret.ToString(), output.ToString());
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

        [TestCase("a",  "kissa", ErrorType.TypeError)]
        [TestCase("a",  "666")]
        [TestCase("b", "kissa")]
        [TestCase("b",  "kissa koira", ErrorType.InputError)]
        [TestCase("a",  "666 8", ErrorType.InputError)]
        [TestCase("c",  "667", ErrorType.TypeError)]
        public void StatementNodeReadTests(string id, string input, ErrorType errorType = ErrorType.Unknown)
        {
            var mockNode = new Mock<VariableNode>();
            var argumentToken = Token.Of(
                TokenType.Identifier,
                KeywordType.Unknown,
                id,
                MockSourceInfo
            );
            mockNode.Setup(n => n.Token).Returns(argumentToken);
            var node = CreateStatementNode(KeywordType.Read, mockNode.Object);
            
            var inputStream = new StringReader($"{input}\n");
            Console.SetIn(inputStream);
            
            visitor.Visit(node);

            memoryMock.Verify(m => m.UpdateVariable(id, input, false), 
                errorType != ErrorType.InputError ? Times.Once() : Times.Never());

            if (errorType != ErrorType.Unknown)
            {
                errorServiceMock.Verify(e => e.Add(
                    errorType,
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
                        MockSourceInfo
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

        [Test]
        public void NoOpNodeTest() => Assert.Null(visitor.Visit(new NoOpNode()));
        
        [Test]
        public void StatementListTest()
        {
            var mockNode = new Mock<StatementNode>();

            var node = new StatementListNode
            {
                Left = mockNode.Object,
                Right = mockNode.Object
            };

            visitor.Visit(node);

            mockNode.Verify(n => n.Accept(visitor), Times.Exactly(2));
        }

        [Test]
        public void ExpressionNodeTest()
        {
            var mockNode = new Mock<LiteralNode>();

            var node = new ExpressionNode
            {
                Expression = mockNode.Object
            };

            var ret = visitor.Visit(node);

            mockNode.Verify(n => n.Accept(visitor), Times.Once);
        }

        [TestCase(1, 2, "+", 3)]
        [TestCase(-1, 1, "+", 0)]
        [TestCase("kissa", "koira", "+", "kissakoira")]
        [TestCase("", "koira", "+", "koira")]
        [TestCase(3, 7, "-", -4)]
        [TestCase(2, 6, "*", 12)]
        [TestCase(6, 2, "/", 3)]
        [TestCase(3, 2, "/", 1)]
        [TestCase(true, true, "&", true)]
        [TestCase(false, true, "&", false)]
        [TestCase(3, 6, "<", true)]
        [TestCase(6, 3, "<", false)]
        [TestCase(6, 6, "<", false)]
        [TestCase("aaa", "aab", "<", true)]
        [TestCase("aab", "aaa", "<", false)]
        [TestCase("aaa", "aaa", "<", false)]
        [TestCase(true, false, "<", false)]
        [TestCase(false, true, "<", true)]
        [TestCase(false, false, "<", false)]
        [TestCase(1, 1, "=", true)]
        [TestCase(1, 2, "=", false)]
        [TestCase("kissa", "kissa", "=", true)]
        [TestCase("kissa", "koira", "=", false)]
        [TestCase(true, true, "=", true)]
        [TestCase(true, false, "=", false)]
        [TestCase(1, "kissa", "+", null, true)]
        [TestCase("kissa", "koira", "-", null, true)]
        [TestCase(true, false, "*", null, true)]
        [TestCase(2, 3, "&", null, true)]
        [TestCase(2, true, "<", null, true)]
        public void BinaryNodeTests(object left, object right, string op, object expected, bool error = false)
        {
            var leftNodeMock = new Mock<LiteralNode>();
            var rightNodeMock = new Mock<LiteralNode>();
            leftNodeMock.Setup(m => m.Accept(visitor)).Returns(left);
            rightNodeMock.Setup(m => m.Accept(visitor)).Returns(right);

            var nodeToken = Token.Of(
                TokenType.Operator,
                KeywordType.Unknown,
                op,
                MockSourceInfo
            );
            var node = new BinaryNode
            {
                Token = nodeToken,
                Left = leftNodeMock.Object,
                Right = rightNodeMock.Object,
            };

            var ret = visitor.Visit(node);
            
            leftNodeMock.Verify(m => m.Accept(visitor), Times.Once);
            rightNodeMock.Verify(m => m.Accept(visitor), Times.Once);
            
            if (error)
            {
                errorServiceMock.Verify(m => m.Add(
                    ErrorType.InvalidOperation,
                    nodeToken,
                    $"invalid binary operation {op}",
                    false
                ), Times.Once);
            }
            Assert.AreEqual(expected, ret);
        }

        [TestCase("-", 1, -1)]
        [TestCase("+", 1, null, true)]
        [TestCase("*", true, null, true)]
        [TestCase("-", "kissa", null, true)]
        [TestCase("!", true, false)]
        [TestCase("!", false, true)]
        [TestCase("!", 6, null, true)]
        public void UnaryNodeTests(string op, object value, object expected, bool error = false)
        {
            var nodeMock = new Mock<LiteralNode>();
            nodeMock.Setup(n => n.Accept(visitor)).Returns(value);
            var nodeToken = Token.Of(
                TokenType.Operator,
                KeywordType.Unknown,
                op,
                MockSourceInfo
            );

            var node = new UnaryNode
            {
                Token = nodeToken,
                Value = nodeMock.Object
            };

            var ret = visitor.Visit(node);

            if (error)
            {
                errorServiceMock.Verify(e => e.Add(
                    ErrorType.InvalidOperation,
                    nodeToken,
                    $"invalid unary operator {op}",
                    false
                ), Times.Once);
            }
            
            Assert.AreEqual(expected, ret);
        }

        [TestCase("a", PrimitiveType.Int, 6, 6)]
        [TestCase("a", PrimitiveType.Int, null, 0)]
        [TestCase("b", PrimitiveType.String, "kissa", "kissa")]
        [TestCase("b", PrimitiveType.String, null, "")]
        [TestCase("c", PrimitiveType.Bool, true, true)]
        [TestCase("c", PrimitiveType.Bool, null, false)]
        public void AssignmentNodeTests(string id, PrimitiveType type, object value, object expected)
        {
            var nodeMock = new Mock<ExpressionNode>();
            nodeMock.Setup(n => n.Accept(visitor)).Returns(value);
            var idToken = Token.Of(
                TokenType.Identifier,
                KeywordType.Unknown,
                id,
                MockSourceInfo
            );
            var node = new AssignmentNode
            {
                Id = new VariableNode
                {
                    Token = idToken
                },
                Expression = nodeMock.Object,
                Type = type
            };

            var ret = visitor.Visit(node);
            
            memoryMock.Verify(m => m.UpdateVariable(id, expected, false), Times.Once);
            Assert.AreEqual(expected, ret);
        }

        [TestCase(PrimitiveType.Int, TokenType.IntValue, "6")]
        [TestCase(PrimitiveType.String, TokenType.StringValue, "kissa")]
        [TestCase(PrimitiveType.Bool, TokenType.BoolValue, "false")]
        public void LiteralNodeTests(PrimitiveType type, TokenType tt, string content)
        {
            var node = new LiteralNode
            {
                Type = type,
                Token = Token.Of(
                    tt,
                    KeywordType.Unknown,
                    content,
                    MockSourceInfo
                )
            };
            visitor.Visit(node);
            
            memoryMock.Verify(m => m.ParseResult(type, content), Times.Once);
        }

        [TestCase("a")]
        [TestCase("a", true)]
        public void VariableNodeTests(string id, bool error = false)
        {
            var nodeToken = Token.Of(
                TokenType.Identifier,
                KeywordType.Unknown,
                id,
                MockSourceInfo
            );
            var node = new VariableNode
            {
                Token = nodeToken
            };
            symbolTableMock.Setup(s => s.SymbolExists(id)).Returns(!error);
            
            visitor.Visit(node);

            if (error)
            {
                errorServiceMock.Verify(e => e.Add(
                    ErrorType.UndeclaredVariable,
                    nodeToken,
                    It.IsAny<string>(),
                    false
                ));
            }
            memoryMock.Verify(m => m.LookupVariable(id), Times.Once);
        }
    }
}