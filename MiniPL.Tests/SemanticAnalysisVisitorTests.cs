using System;
using System.Collections.Generic;
using MiniPL.Common;
using MiniPL.Common.AST;
using MiniPL.Common.Errors;
using MiniPL.Common.Symbols;
using MiniPL.Interpret;
using Moq;
using NUnit.Framework;
using static MiniPL.Tests.TestUtil;

namespace MiniPL.Tests
{
    public class SemanticAnalysisVisitorTests
    {
        private ISymbolTable symbolTable;
        private Mock<SymbolTable> symbolTableMock;
        private Mock<IErrorService> errorServiceMock;
        private SemanticAnalysisVisitor visitor;
        
        [SetUp]
        public void Setup()
        {
            symbolTableMock = new Mock<SymbolTable>
            {
                CallBase = true
            };

            symbolTable = symbolTableMock.Object;
            symbolTable.DeclareSymbol("a", PrimitiveType.Int);
            symbolTable.DeclareSymbol("b", PrimitiveType.Int);
            symbolTable.DeclareSymbol("c", PrimitiveType.String);
            symbolTable.DeclareSymbol("d", PrimitiveType.Bool);
            symbolTable.SetControlVariable("a");

            errorServiceMock = new Mock<IErrorService>();
            
            Context.SymbolTable = symbolTable;
            Context.ErrorService = errorServiceMock.Object;
            
            visitor = new SemanticAnalysisVisitor();
        }

        [TearDown]
        public void Teardown()
        {
            Context.SymbolTable = null;
            Context.ErrorService = null;
        }

        [TestCase(KeywordType.Print, "a")]
        [TestCase(KeywordType.Print, "b")]
        [TestCase(KeywordType.Print, "c")]
        [TestCase(KeywordType.Print, "d", ErrorType.TypeError)]
        [TestCase(KeywordType.Assert, "d")]
        [TestCase(KeywordType.Assert, "b", ErrorType.TypeError)]
        [TestCase(KeywordType.Read, "a", ErrorType.AssignmentToControlVariable, true)]
        [TestCase(KeywordType.Read, "b")]
        [TestCase(KeywordType.Read, "c")]
        [TestCase(KeywordType.Read, "d", ErrorType.TypeError)]
        public void StatementTests(KeywordType kwt, string id = null, ErrorType errorType = ErrorType.Unknown, bool critical = false)
        {
            var error = errorType != ErrorType.Unknown;
            var type = id != null ? symbolTable.LookupSymbol(id) : PrimitiveType.Void;
            
            var argument = new Mock<VariableNode>();
            var idToken = Token.Of(
                TokenType.Identifier,
                KeywordType.Unknown,
                id,
                MockSourceInfo
            );
            argument.Setup(a => a.Token).Returns(idToken);
            argument.Setup(a => a.Accept(It.IsAny<SemanticAnalysisVisitor>())).Returns(type);

            var node = new StatementNode
            {
                Token = Token.Of(TokenType.Keyword, kwt, "", MockSourceInfo),
                Arguments = new List<Node>
                {
                    argument.Object
                }
            };
            
            var ret = visitor.Visit(node);

            if (error)
            {
                errorServiceMock.Verify(es => es.Add(
                    errorType,
                    idToken,
                    It.IsAny<string>(),
                    critical
                ));
                Assert.AreEqual(null, ret);
            }
            else
            {
                Assert.AreEqual(type, ret);                
            }
            
            argument.Verify(l => l.Accept(visitor), Times.Once());
        }

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

        [TestCase("+", PrimitiveType.Int, PrimitiveType.Int, PrimitiveType.Int)]
        [TestCase("+", PrimitiveType.Int, PrimitiveType.String, PrimitiveType.Int, true)]
        [TestCase("-", PrimitiveType.Bool, PrimitiveType.Int, PrimitiveType.Bool, true)]
        [TestCase("=", PrimitiveType.Int, PrimitiveType.Int, PrimitiveType.Bool)]
        [TestCase("&", PrimitiveType.Int, PrimitiveType.Int, PrimitiveType.Bool)]
        [TestCase("<", PrimitiveType.Int, PrimitiveType.Int, PrimitiveType.Bool)]
        public void BinaryNodeTest(string op, PrimitiveType type1, PrimitiveType type2, PrimitiveType expectedType, bool error = false)
        {
            var mockNode = new Mock<LiteralNode>();
            mockNode.SetupSequence(n => n.Accept(visitor))
                .Returns(type1)
                .Returns(type2);
            var token = Token.Of(TokenType.Operator, KeywordType.Unknown, op, MockSourceInfo);

            var node = new BinaryNode
            {
                Token = token,
                Left = mockNode.Object,
                Right = mockNode.Object
            };
            Assert.AreEqual(PrimitiveType.Void, node.Type);

            var ret = visitor.Visit(node);

            if (error)
            {
                errorServiceMock.Verify(es => es.Add(
                    ErrorType.TypeError,
                    token,
                    $"type error: can't perform operation {op} on {type1} and {type2}",
                    false
                ), Times.Once);
            }
            mockNode.Verify(n => n.Accept(visitor), Times.Exactly(2));
            
            Assert.AreEqual(expectedType, node.Type);
        }

        [TestCase("-", PrimitiveType.Int, PrimitiveType.Int)]
        [TestCase("!", PrimitiveType.Bool, PrimitiveType.Bool)]
        [TestCase("-", PrimitiveType.Bool, PrimitiveType.Bool, true)]
        [TestCase("!", PrimitiveType.Int, PrimitiveType.Int, true)]
        public void UnaryNodeTest(string op, PrimitiveType type, PrimitiveType expectedType, bool error = false)
        {
            var mockNode = new Mock<LiteralNode>();
            mockNode.Setup(n => n.Accept(visitor)).Returns(type);

            var token = Token.Of(
                TokenType.Operator,
                KeywordType.Unknown,
                op,
                MockSourceInfo
            ); 
            var node = new UnaryNode
            {
                Token = token,
                Value = mockNode.Object
            };

            var ret = visitor.Visit(node);

            if (error)
            {
                errorServiceMock.Verify(es => es.Add(
                    ErrorType.TypeError,
                    token,
                    $"type error: can't perform operation {op} on {type}",
                    false
                ), Times.Once);
            }
            mockNode.Verify(n => n.Accept(visitor), Times.Once);
            Assert.AreEqual(expectedType, ret);
        }

        [TestCase("a", true, PrimitiveType.Int, PrimitiveType.Int, ErrorType.RedeclaredVariable)]
        [TestCase("a", false, PrimitiveType.Void, PrimitiveType.Int, ErrorType.AssignmentToControlVariable)]
        [TestCase("c", true, PrimitiveType.Int, PrimitiveType.Int)]
        [TestCase("c", true, PrimitiveType.String, PrimitiveType.Int, ErrorType.TypeError)]
        [TestCase("e", false, PrimitiveType.Void, PrimitiveType.Int, ErrorType.UndeclaredVariable)]
        public void AssignmentNodeTest(string id, bool isVar, PrimitiveType type, PrimitiveType expressionType, params ErrorType[] ets)
        {
            var mockNode = new Mock<LiteralNode>();
            mockNode.Setup(n => n.Type).Returns(expressionType);
            mockNode.Setup(n => n.Token).Returns(Token.Of(
                TokenType.Unknown,
                KeywordType.Unknown,
                "",
                MockSourceInfo));
            
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
                Token = Token.Of(
                    TokenType.Assignment,
                    isVar ? KeywordType.Var : KeywordType.Unknown,
                    "",
                    MockSourceInfo
                ),
                Type = type,
                Expression = mockNode.Object
            };

            visitor.Visit(node);

            foreach (var et in ets)
            {
                errorServiceMock.Verify(es => es.Add(
                    et,
                    idToken,
                    It.IsAny<string>(),
                    false
                ));
            }

            var declaredType = isVar ? type : symbolTable.LookupSymbol(id);
            
            symbolTableMock.Verify(s => s.DeclareSymbol(id, declaredType));

            Assert.AreEqual(declaredType, node.Type);
            Assert.AreEqual(declaredType, node.Id.Type);
            
        }

        [TestCase("a", PrimitiveType.Int)]
        [TestCase("e", PrimitiveType.Void, true)]
        public void VariableNodeTest(string id, PrimitiveType expectedType, bool error = false)
        {
            var node = new VariableNode
            {
                Token = Token.Of(
                    TokenType.Identifier,
                    KeywordType.Unknown,
                    id,
                    MockSourceInfo),
            };

            var ret = visitor.Visit(node);

            if (error)
            {
                errorServiceMock.Verify(e => e.Add(
                    ErrorType.UndeclaredVariable,
                    node.Token,
                    It.IsAny<string>(),
                    false
                ));
            }

            symbolTableMock.Verify(s => s.LookupSymbol(id));
            Assert.AreEqual(expectedType, node.Type);
            Assert.AreEqual(expectedType, ret);
        }

        [Test]
        public void LiteralNodeTest()
        {
            var node = new LiteralNode
            {
                Type = PrimitiveType.Int
            };
            Assert.AreEqual(PrimitiveType.Int, visitor.Visit(node));
        }

        [Test]
        public void NoOpNodeTest()
        {
            Assert.AreEqual(PrimitiveType.Void, visitor.Visit(new NoOpNode()));
        }

        [Test]
        public void ForNodeTest()
        {
            var tempSymbolTableMock = new Mock<ISymbolTable>();
            Context.SymbolTable = tempSymbolTableMock.Object;
            
            var id = "a";

            var mockNode = new Mock<LiteralNode>();
            var mockIdNode = new Mock<VariableNode>();
            
            mockIdNode.Setup(n => n.Token).Returns(Token.Of(
                TokenType.Identifier,
                KeywordType.Unknown,
                id,
                MockSourceInfo)
            );
            
            var node = new ForNode
            {
                Id = mockIdNode.Object,
                RangeStart = mockNode.Object,
                RangeEnd = mockNode.Object,
                Statements = mockNode.Object
            };
            
            visitor.Visit(node);

            mockIdNode.Verify(n => n.Accept(visitor), Times.Once);
            mockNode.Verify(n => n.Accept(visitor), Times.Exactly(3));
            tempSymbolTableMock.Verify(s => s.SetControlVariable(id), Times.Once);
            tempSymbolTableMock.Verify(s => s.UnsetControlVariable(id), Times.Once);
        }

        [Test]
        public void ExpressionNodeTest()
        {
            var mockNode = new Mock<LiteralNode>();
            mockNode.Setup(n => n.Accept(visitor)).Returns(PrimitiveType.Int);
            
            var node = new ExpressionNode
            {
                Expression = mockNode.Object
            };

            var ret = visitor.Visit(node);
            
            mockNode.Verify(n => n.Accept(visitor), Times.Once);
            Assert.AreEqual(PrimitiveType.Int, ret);
        }
    }
}