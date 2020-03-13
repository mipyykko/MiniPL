using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Compiler.Common;
using Compiler.Common.AST;
using Compiler.Scan;
using Node = Compiler.Common.AST.Node;

namespace Parse
{
    public class Parser
    {
        private readonly Scanner scanner;

        private Token InputToken;

        // private Node tree;
        private bool DoBlock { get; set; } = false;

        private KeywordType InputTokenKeywordType => InputToken.KeywordType;
        private TokenType InputTokenType => InputToken.Type;
        private string InputTokenContent => InputToken.Content;

        private static readonly Node NoOpStatement = new NoOpNode();

        public Parser(Scanner scanner)
        {
            /* TODO:
               get rid of those tuple things, or find some other thing
               to get ut of that statement function after some of the parts
               fails - now it leads to cascading errors
            */
            this.scanner = scanner;
        }

        private static void ParseError(string error)
        {
            Console.WriteLine(error);
            throw new SyntaxErrorException(error);
            // Console.WriteLine(error);
        }

        private string GetLine => scanner.Text.Lines[InputToken.SourceInfo.LineRange.Line];

        private string GetErrorPosition => new string(' ', Math.Max(0, InputToken.SourceInfo.LineRange.Start))
                                           + new string('^', Math.Max(1, InputTokenContent.Length));

        private void UnexpectedKeywordError(params KeywordType[] kwts)
        {
            var sb = new StringBuilder($"{GetLine}\n{GetErrorPosition}\n");
            sb.Append(
                kwts.Length switch
                {
                    0 => $"unexpected keyword {InputTokenContent} of type {InputTokenKeywordType}",
                    1 => $"expected keyword of type {kwts[0]}, got {InputTokenContent} of type {InputTokenKeywordType}",
                    _ =>
                    $"expected one of keyword types {string.Join(", ", kwts)}, got {InputTokenContent} of type {InputTokenKeywordType}"
                }
            );
            SkipToTokenType(TokenType.Separator);
            ParseError(sb.ToString());
        }

        private void UnexpectedTokenError(params TokenType[] tts)
        {
            var sb = new StringBuilder($"{GetLine}\n{GetErrorPosition}\n");
            sb.Append(
                tts.Length switch
                {
                    0 => $"unexpected token {InputTokenContent} of type {InputTokenType}",
                    1 => $"expected token of type {tts[0]}, got {InputTokenContent} of type {InputTokenType}",
                    _ =>
                    $"expected one of token types {string.Join(", ", tts)}, got {InputTokenContent} of type {InputTokenType}"
                }
            );
            SkipToTokenType(TokenType.Separator);
            ParseError(sb.ToString());
        }

        private void NextToken() => InputToken = scanner.GetNextToken();

        private static readonly KeywordType[] DefaultStatementFirstKeywords =
        {
            KeywordType.Var,
            KeywordType.For,
            KeywordType.Read,
            KeywordType.Print,
            KeywordType.Assert
        };

        private static readonly KeywordType[] DoBlockStatementFirstKeywords =
            DefaultStatementFirstKeywords.Concat(new[] {KeywordType.End}).ToArray();

        private KeywordType[] StatementFirstKeywords =>
            DoBlock
                ? DoBlockStatementFirstKeywords
                : DefaultStatementFirstKeywords;

        public Node Program()
        {
            NextToken();

            var (statements, _) = (
                StatementList(),
                MatchTokenType(TokenType.EOF)
            );
            return new StatementListNode
            {
                Left = statements,
                Right = new NoOpNode()
            };
        }


        private void SkipToTokenType(TokenType tt)
        {
            while (InputTokenType != tt && InputTokenType != TokenType.EOF) NextToken();
            if (InputTokenType != TokenType.EOF) NextToken();
        }

        private Node StatementList()
        {
            try
            {
                switch (InputTokenType)
                {
                    case TokenType.Keyword when DoBlock && InputTokenKeywordType == KeywordType.End:
                    case TokenType.EOF:
                        return NoOpStatement;
                    case TokenType.Keyword when StatementFirstKeywords.Includes(InputTokenKeywordType):
                    case TokenType.Identifier:
                        return StatementStatementList();
                    default:
                    {
                        UnexpectedKeywordError(StatementFirstKeywords);
                        return StatementStatementList();
                    }
                }
            }
            catch (Exception)
            {
                return StatementStatementList();
            }
        }

        private Node StatementStatementList()
        {
            var (left, _, right) = (
                Statement(),
                Separator(),
                StatementList()
            );
            return new StatementListNode
            {
                Left = left,
                Right = right
            };
        }

        private Token Separator() => MatchTokenType(TokenType.Separator);

        private TokenType[] StatementFirstTokens = new[]
        {
            TokenType.Keyword,
            TokenType.Identifier
        };

        private Node Statement()
        {
            try
            {
                switch (InputTokenType)
                {
                    case TokenType.Keyword:
                    {
                        var statement = InputTokenKeywordType switch
                        {
                            KeywordType.Var => VarStatement(),
                            KeywordType.For => ForStatement(),
                            KeywordType.Read => ReadStatement(),
                            KeywordType.Print => PrintStatement(),
                            KeywordType.Assert => AssertStatement(),
                            KeywordType.End when DoBlock => new NoOpNode(),
                            _ => null // TODO: ErrorStatement?
                        };
                        if (statement == null)
                        {
                            UnexpectedKeywordError(StatementFirstKeywords);
                        }

                        return statement;
                    }
                    case TokenType.Identifier:
                        return AssignmentStatement();
                    default:
                        UnexpectedTokenError(StatementFirstTokens);
                        break;
                }

                return null;
            }
            catch (Exception)
            {
                Console.WriteLine("caught");
                return NoOpStatement;
            }
        }

        private Node DoEndBlock(KeywordType expectedEnd)
        {
            DoBlock = true;

            var (_, statements, __, ___) = (
                MatchKeywordType(KeywordType.Do),
                StatementList(),
                MatchKeywordType(KeywordType.End),
                MatchKeywordType(expectedEnd) // ie. started with for, end with "end for";
            );

            DoBlock = false;

            return statements;
        }

        private Node AssignmentStatement()
        {
            var (id, _, expr) = (
                MatchTokenType(TokenType.Identifier),
                MatchTokenType(TokenType.Assignment),
                Expression()
            );

            return new AssignmentNode
            {
                Id = new VariableNode
                {
                    Token = id
                },
                Expression = expr
            };
        }

        private Node AssertStatement()
        {
            var (token, _, expr, __) = (
                MatchKeywordType(KeywordType.Assert),
                MatchTokenType(TokenType.OpenParen),
                Expression(),
                MatchTokenType(TokenType.CloseParen)
            );
            return new StatementNode
            {
                Token = token,
                Arguments = new List<Node> {expr}
            };
        }

        private Node PrintStatement()
        {
            var (token, value) = (
                MatchKeywordType(KeywordType.Print),
                Expression()
            );

            return new StatementNode
            {
                Token = token,
                Arguments = new List<Node> {value}
            };
        }

        private Node ReadStatement()
        {
            var (token, id) = (
                MatchKeywordType(KeywordType.Read),
                MatchTokenType(TokenType.Identifier)
            );

            return new StatementNode
            {
                Token = token,
                Arguments = new List<Node>
                {
                    new VariableNode
                    {
                        Token = id
                    }
                }
            };
        }

        private Node ForStatement()
        {
            var (_, id, __, rangeStart, ___, rangeEnd, statements) = (
                MatchKeywordType(KeywordType.For),
                MatchTokenType(TokenType.Identifier),
                MatchKeywordType(KeywordType.In),
                Expression(),
                MatchTokenType(TokenType.Range),
                Expression(),
                DoEndBlock(KeywordType.For)
            );

            return new ForNode
            {
                Token = id,
                RangeStart = rangeStart,
                RangeEnd = rangeEnd,
                Statements = statements
            };
        }

        private Node VarStatement()
        {
            var (token, id, _, type) = (
                MatchKeywordType(KeywordType.Var),
                MatchTokenType(TokenType.Identifier),
                MatchTokenType(TokenType.Colon),
                Type()
            );

            var n = new AssignmentNode
            {
                Token = token,
                Id = new VariableNode
                {
                    Token = id
                },
                Type = type,
            };
            if (InputTokenType == TokenType.Separator)
            {
                n.Expression = NoOpStatement;
                return n;
            }

            var (_, value) = (
                MatchTokenType(TokenType.Assignment),
                Expression()
            );

            n.Expression = value;
            return n;
        }

        private PrimitiveType Type()
        {
            KeywordType[] expectedTypes =
            {
                KeywordType.Int,
                KeywordType.String,
                KeywordType.Bool
            };
            var tt = MatchKeywordType(expectedTypes);

            if (tt != null) return tt.KeywordType.ToPrimitiveType();

            UnexpectedKeywordError(expectedTypes);
            return PrimitiveType.Void;
        }

        private Node Expression()
        {
            switch (InputTokenType)
            {
                case TokenType.IntValue:
                case TokenType.StringValue:
                case TokenType.BoolValue:
                case TokenType.Identifier:
                case TokenType.OpenParen:
                {
                    var opnd1 = Operand();
                    if (InputTokenType == TokenType.Operator)
                    {
                        var op = Operator();
                        var opnd2 = Operand();

                        return new ExpressionNode
                        {
                            Expression = new BinaryNode
                            {
                                Left = opnd1,
                                Token = op,
                                Right = opnd2
                            }
                        };
                    }

                    return opnd1;
                }
                case TokenType.Operator:
                {
                    var op = UnaryOperator();
                    var opnd = Operand();

                    return new ExpressionNode
                    {
                        Expression = new UnaryNode
                        {
                            Token = op,
                            Value = opnd
                        }
                    };
                }
                default:
                    UnexpectedTokenError(null); // TODO
                    break;
            }

            return null;
            // return Node.Of(NodeType.Unknown);
        }

        private Node Operand()
        {
            switch (InputTokenType)
            {
                case TokenType.IntValue:
                case TokenType.StringValue:
                case TokenType.BoolValue:
                {
                    var token = MatchTokenType(InputTokenType);
                    return new LiteralNode
                    {
                        Token = token,
                        Type = TokenToPrimitiveType.TryGetValueOrDefault(token.Type)
                    };
                }
                case TokenType.Identifier:
                {
                    var t = MatchTokenType(InputTokenType);
                    return new VariableNode
                    {
                        Token = t
                    };
                }
                case TokenType.OpenParen:
                    var (_, n, __) = (
                        MatchTokenType(TokenType.OpenParen),
                        Expression(),
                        MatchTokenType(TokenType.CloseParen)
                    );
                    return n;
                default:
                    UnexpectedTokenError(null); // TODO
                    break;
            }

            return null; //Node.Of(NodeType.Unknown);
        }

        public static Dictionary<TokenType, PrimitiveType> TokenToPrimitiveType =
            new Dictionary<TokenType, PrimitiveType>()
            {
                [TokenType.IntValue] = PrimitiveType.Int,
                [TokenType.StringValue] = PrimitiveType.String,
                [TokenType.BoolValue] = PrimitiveType.Bool,
            };

        private Token Operator() => MatchTokenType(TokenType.Operator);

        private Token UnaryOperator() // TODO: this was a bit wonky 
        {
            var t = MatchTokenType(TokenType.Operator);

            if (t.Content.Equals("!") || t.Content.Equals("-")) return t;

            UnexpectedTokenError(t.Type); // TODO: not actually correct
            return t;
        }

        private Token MatchTokenType(TokenType tt)
        {
            var matchedToken = InputToken;
            if (InputTokenType == tt)
            {
                NextToken();
                Console.WriteLine("matched token {0}", tt);
                return matchedToken;
            }

            UnexpectedTokenError(tt);
            //NextToken();
            return matchedToken;
        }

        public Token MatchTokenType(params TokenType[] tts)
        {
            foreach (var tt in tts)
                if (InputTokenType == tt)
                    return MatchTokenType(tt);
            UnexpectedTokenError(null); // TODO: one of
            return null;
        }

        private Token MatchKeywordType(KeywordType kwt)
        {
            if (InputTokenKeywordType == kwt)
            {
                var matchedToken = InputToken;
                NextToken();
                Console.WriteLine("matched keyword {0}", kwt);
                return matchedToken;
            }

            UnexpectedKeywordError(kwt);
            return null;
        }

        private Token MatchKeywordType(KeywordType[] kwtl)
        {
            foreach (var kwt in kwtl)
                if (InputTokenKeywordType == kwt)
                {
                    var matchedToken = InputToken;
                    NextToken();
                    Console.WriteLine("matched keyword {0}", kwt);
                    return matchedToken;
                }

            UnexpectedKeywordError(kwtl);
            return null;
        }

        private bool MatchContent(string s)
        {
            if (InputTokenContent.Equals(s)) return true;
            ParseError($"expected {s}");
            return false;
        }

        public bool MatchContent(string[] sl)
        {
            foreach (var s in sl)
                if (!MatchContent(s))
                    return false;
            return true;
        }
    }
}