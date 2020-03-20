using System.Collections.Generic;
using System.Data;
using Compiler.Common;
using Compiler.Common.AST;
using Compiler.Common.Errors;
using Moq;
using NUnit.Framework;

namespace Compiler.Tests
{
    public class SymbolTableVisitorTests
    {
        private ISymbolTable symbolTable;
        private Mock<SymbolTable> symbolTableMock;
        private Mock<IErrorService> errorServiceMock;
        private SymbolTableVisitor visitor;
        
        [SetUp]
        public void Setup()
        {
            symbolTableMock = new Mock<SymbolTable>();

            symbolTableMock.CallBase = true;
            symbolTable = symbolTableMock.Object;
            symbolTable.DeclareSymbol("a", PrimitiveType.Int);
            symbolTable.DeclareSymbol("b", PrimitiveType.Int);
            symbolTable.SetControlVariable("a");

            errorServiceMock = new Mock<IErrorService>();
            
            Context.SymbolTable = symbolTable;
            Context.ErrorService = errorServiceMock.Object;
            
            visitor = new SymbolTableVisitor();
        }

        [TestCase(KeywordType.Print)]
        [TestCase(KeywordType.Assert)]
        [TestCase(KeywordType.Read, "a", true)]
        [TestCase(KeywordType.Read, "b")]
        public void StatementTests(KeywordType kwt, string id = null, bool error = false)
        {
            var argument = new Mock<VariableNode>();
            var idToken = Token.Of(
                TokenType.Identifier,
                KeywordType.Unknown,
                id,
                SourceInfo.Of((0, 0), (0, 0, 0))
            );
            argument.Setup(a => a.Token).Returns(idToken);
            argument.Setup(a => a.Accept(It.IsAny<SymbolTableVisitor>())).Returns("ok");

            var node = new StatementNode
            {
                Token = Token.Of(TokenType.Keyword, kwt, "", SourceInfo.Of((0,0), (0,0,0))),
                Arguments = new List<Node>
                {
                    argument.Object
                }
            };
            
            var ret = visitor.Visit(node);

            if (error)
            {
                errorServiceMock.Verify(es => es.Add(
                    ErrorType.AssignmentToControlVariable,
                    idToken,
                    It.IsAny<string>(),
                    true
                ));
            }
            
            argument.Verify(l => l.Accept(visitor),
                error ? Times.Never() : Times.Once());
            Assert.AreEqual(error ? null : "ok", ret);
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

        [TestCase(PrimitiveType.Int, PrimitiveType.Int)]
        [TestCase(PrimitiveType.Int, PrimitiveType.String, true)]
        [TestCase(PrimitiveType.Bool, PrimitiveType.Int, true)]
        public void BinaryNodeTest(PrimitiveType type1, PrimitiveType type2, bool error = false)
        {
            var mockNode = new Mock<LiteralNode>();
            mockNode.SetupSequence(n => n.Accept(visitor))
                .Returns(type1)
                .Returns(type2);
            var token = Token.Of(TokenType.Operator, KeywordType.Unknown, "+", SourceInfo.Of((0, 0), (0, 0, 0)));

            var node = new BinaryNode
            {
                Token = token,
                Left = mockNode.Object,
                Right = mockNode.Object
            };
            Assert.AreEqual(PrimitiveType.Void, node.Type);

            visitor.Visit(node);

            if (error)
            {
                errorServiceMock.Verify(es => es.Add(
                    ErrorType.TypeError,
                    token,
                    $"type error: can't perform operation + on {type1} and {type2}",
                    false
                ));
            }
            mockNode.Verify(n => n.Accept(visitor), Times.Exactly(2));
            
            Assert.AreEqual(type1, node.Type);
        }

        [Test]
        public void UnaryNodeTest()
        {
            var mockNode = new Mock<LiteralNode>();
            mockNode.Setup(n => n.Accept(visitor)).Returns(PrimitiveType.Int);

            var node = new UnaryNode
            {
                Value = mockNode.Object
            };

            var ret = visitor.Visit(node);
            
            mockNode.Verify(n => n.Accept(visitor), Times.Once);
            Assert.AreEqual(PrimitiveType.Int, ret);
        }

        [TestCase("a", true, PrimitiveType.Int, PrimitiveType.Int, ErrorType.RedeclaredVariable)]
        [TestCase("a", false, PrimitiveType.Void, PrimitiveType.Int, ErrorType.AssignmentToControlVariable)]
        [TestCase("c", true, PrimitiveType.Int, PrimitiveType.Int)]
        [TestCase("c", true, PrimitiveType.String, PrimitiveType.Int, ErrorType.TypeError)]
        [TestCase("c", false, PrimitiveType.Void, PrimitiveType.Int, ErrorType.UndeclaredVariable)]
        public void AssignmentNodeTest(string id, bool isVar, PrimitiveType type, PrimitiveType expressionType, params ErrorType[] ets)
        {
            var mockNode = new Mock<LiteralNode>();
            mockNode.Setup(n => n.Type).Returns(expressionType);
            mockNode.Setup(n => n.Token).Returns(Token.Of(
                TokenType.Unknown,
                KeywordType.Unknown,
                "",
                SourceInfo.Of((0, 0), (0, 0, 0))));
            
            var idToken = Token.Of(
                TokenType.Identifier,
                KeywordType.Unknown,
                id,
                SourceInfo.Of((0, 0), (0, 0, 0))
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
                    SourceInfo.Of((0,0), (0,0,0))
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
        [TestCase("c", PrimitiveType.Void, true)]
        public void VariableNodeTest(string id, PrimitiveType expectedType, bool error = false)
        {
            var node = new VariableNode
            {
                Token = Token.Of(
                    TokenType.Identifier,
                    KeywordType.Unknown,
                    id,
                    SourceInfo.Of((0, 0), (0, 0, 0))),
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
            var ret = visitor.Visit(node);
            Assert.AreEqual(PrimitiveType.Int, ret);
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
                SourceInfo.Of((0, 0), (0, 0, 0)))
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