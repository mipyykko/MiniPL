using System;
using System.Collections.Generic;
using System.Text;
using Compiler.Common;
using Compiler.Common.AST;
using Compiler.Scan;
using NodeType = Compiler.Common.Node.NodeType;
using Node = Compiler.Common.AST.Node;

namespace Parse
{
    public class Parser
    {
        private readonly Scanner scanner;
        private Token InputToken;
        // private Node tree;
        private bool DoBlock { get; set; } = false;

        private KeywordType TokenKeywordType => InputToken.KeywordType;
        private TokenType TokenType => InputToken.Type;
        private string TokenContent => InputToken.Content;

        private readonly KeywordType[] StatementFirstKeywords =
        {
            KeywordType.Var,
            KeywordType.For,
            KeywordType.Read,
            KeywordType.Print,
            KeywordType.Assert
        };

        public Parser(Scanner scanner)
        {
            this.scanner = scanner;
        }


        public void Parse()
        {
            Program();
        }

        public void ParseError(string error)
        {
            Console.WriteLine(error);
        }

        private string GetLine => scanner.Text.Lines[InputToken.SourceInfo.LineRange.Line];

        private string GetErrorPosition => new string(' ', Math.Max(0, InputToken.SourceInfo.LineRange.Start))
                                           + new string('^', Math.Max(0, TokenContent.Length));

        public void UnexpectedKeywordError(params KeywordType[] kwts)
        {
            var sb = new StringBuilder($"{GetLine}\n{GetErrorPosition}\n");
            if (kwts.Length == 0) sb.Append($"unexpected keyword {TokenContent} of type {TokenKeywordType}");
            if (kwts.Length == 1)
                sb.Append($"expected keyword of type {kwts[0]}, got {TokenContent} of type {TokenKeywordType}");
            if (kwts.Length > 1)
                sb.Append(
                    $"expected one of keyword types {string.Join(", ", kwts)}, got {TokenContent} of type {TokenKeywordType}");
            ParseError(sb.ToString());
        }

        public void UnexpectedTokenError(TokenType? tt)
        {
            ParseError($"{GetLine}\n{GetErrorPosition}\nexpected token {tt}, got {TokenType}");
        }

        public void TypeError(NodeType expected, NodeType got)
        {
            ParseError($"{GetLine}\n{GetErrorPosition}\nexpected value of type {expected}, got {got}");
        }

        public void NextToken()
        {
            InputToken = scanner.GetNextToken();
        }

        public Node Program()
        {
            NextToken();

            StatementListNode tree = new StatementListNode
            {
                Left = new NoOpNode(),
                Right = new NoOpNode()
            };
            switch (TokenType)
            {
                case TokenType.Keyword when !StatementFirstKeywords.Includes(TokenKeywordType):
                    UnexpectedKeywordError(StatementFirstKeywords);
                    break;
                case TokenType.Keyword:
                {
                    var statements = StatementList();
                    NextToken();
                    MatchTokenType(TokenType.EOF);
                    tree.Left = statements;
                    // tree.AddChild(Node.Of(NodeType.EOF));
                    break;
                }
                case TokenType.Identifier:
                case TokenType.EOF:
                {
                    var statements = StatementList();
                    MatchTokenType(TokenType.EOF);
                    tree.Left = statements;
                    // tree.AddChild(Node.Of(NodeType.EOF));
                    break;
                }
                default:
                    UnexpectedTokenError(null);
                    break;
            }

            return tree;
            // Node.PrintTree(tree);
        }


        public void SkipToTokenType(TokenType tt)
        {
            while (TokenType != tt && TokenType != TokenType.EOF) NextToken();
            if (TokenType != TokenType.EOF) NextToken();
        }

        public Node StatementList()
        {
            var node = new StatementListNode();

            switch (TokenType)
            {
                case TokenType.Keyword when DoBlock && TokenKeywordType == KeywordType.End:
                case TokenType.EOF: 
                    node.Left = new NoOpNode();
                    node.Right = new NoOpNode();
                    break;
                case TokenType.Keyword when StatementFirstKeywords.Includes(TokenKeywordType):
                    node.Left = Statement();
                    MatchTokenType(TokenType.Separator);
                    node.Right = StatementList();
                    break;
                case TokenType.Keyword:
                    UnexpectedKeywordError(StatementFirstKeywords);
                    SkipToTokenType(TokenType.Separator);
                    node.Left = Statement();
                    MatchTokenType(TokenType.Separator);
                    node.Right = StatementList();
                    break;
                case TokenType.Identifier:
                    node.Left = Statement();
                    MatchTokenType(TokenType.Separator);
                    node.Right = StatementList();
                    break;
                default:
                    UnexpectedTokenError(null);
                    break;
            }

            return node;
        }

        public Node Statement()
        {
            KeywordType[] expected =
            {
                KeywordType.Var, KeywordType.For, KeywordType.Read, KeywordType.Print, KeywordType.Assert
            };

            switch (TokenType)
            {
                case TokenType.Keyword:
                    switch (TokenKeywordType)
                    {
                        case KeywordType.Var:
                            return VarStatement();
                        case KeywordType.For:
                            return ForStatement();
                        case KeywordType.Read:
                            return ReadStatement();
                        case KeywordType.Print:
                            return PrintStatement();
                        case KeywordType.Assert:
                            return AssertStatement();
                        case KeywordType.End when DoBlock:
                            break;
                        default:
                            UnexpectedKeywordError(expected);
                            SkipToTokenType(TokenType.Separator);
                            return null;
                        // break;
                    }

                    break;
                case TokenType.Identifier:
                    return AssignmentStatement();
                default:
                    UnexpectedTokenError(null);
                    break;
            }

            // MatchTokenType(TokenType.Separator);

            return null;
        }

        private Node DoEndBlock(KeywordType expectedEnd)
        {
            DoBlock = true;

            MatchKeywordType(KeywordType.Do);
            var statements = StatementList();
            MatchKeywordType(KeywordType.End);
            MatchKeywordType(expectedEnd); // ie. started with for, end with "end for";

            DoBlock = false;

            return statements;
        }

        private Node AssignmentStatement()
        {
            var id = MatchTokenType(TokenType.Identifier);
            MatchTokenType(TokenType.Assignment);
            var expr = Expression();

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
            var token = MatchKeywordType(KeywordType.Assert);
            MatchTokenType(TokenType.OpenParen);
            var expr = Expression();
            MatchTokenType(TokenType.CloseParen);

            return new StatementNode
            {
                Token = token,
                Arguments = new List<Node> { expr }
            };
        }

        private Node PrintStatement()
        {
            var token = MatchKeywordType(KeywordType.Print);
            var value = Expression();

            return new StatementNode
            {
                Token = token,
                Arguments = new List<Node> { value }
            };
        }

        private Node ReadStatement()
        {
            var token = MatchKeywordType(KeywordType.Read);
            var id = MatchTokenType(TokenType.Identifier);

            Console.WriteLine($"read with id {id.Content}");
            return new StatementNode
            {
                Token = token,
                Arguments = new List<Node> { new VariableNode
                    {
                        Token = id
                    } 
                }
            };
        }

        private Node ForStatement()
        {
            NextToken(); // TODO: match keyword, add variable in ForNode

            var id = MatchTokenType(TokenType.Identifier);
            MatchKeywordType(KeywordType.In);
            var rangeStart = Expression();
            MatchTokenType(TokenType.Range);
            var rangeEnd = Expression();

            var statements = DoEndBlock(KeywordType.For);

            return new ForNode
            {
                Token = id,
                RangeStart = rangeStart,
                RangeEnd = rangeEnd,
                Statements = statements
            };
        }

        public Node VarStatement()
        {
            var token = MatchKeywordType(KeywordType.Var);
            var id = MatchTokenType(TokenType.Identifier);
            MatchTokenType(TokenType.Colon);
            var type = Type();
            
            var n = new AssignmentNode
            {
                Token = token,
                Id = new VariableNode
                {
                    Token = id
                },
                Type = type,
            };
            if (TokenType == TokenType.Separator)
            {
                n.Expression = new NoOpNode();
                return n;
            }

            MatchTokenType(TokenType.Assignment);
            var value = Expression();

            // CheckType(value, type.Type);

            n.Expression = value;
            return n;
        }

        // public void CheckType(Node n, NodeType nt)
        // {
        //     switch (n.Type)
        //     {
        //         case NodeType.ValueExpression:
        //             CheckType(n.Children[0], nt);
        //             break;
        //         case NodeType.BinaryExpression:
        //             CheckType(n.Children[0], nt);
        //             CheckType(n.Children[2], nt);
        //             break;
        //         case NodeType.UnaryExpression:
        //             CheckType(n.Children[1], nt);
        //             break;
        //         default:
        //             if (n.Type != Node.TypeToValue.TryGetValueOrDefault(nt)) TypeError(nt, n.Type);
        //             break;
        //     }
        // }

        public PrimitiveType Type()
        {
            KeywordType[] expectedTypes =
            {
                KeywordType.Int,
                KeywordType.String,
                KeywordType.Bool
            };
            var tt = MatchKeywordTypeList(expectedTypes);

            if (tt == null)
            {
                UnexpectedKeywordError(expectedTypes);
                return PrimitiveType.Void;
            }

            // anytype should never happen after ^
            return tt.KeywordType.ToPrimitiveType();
        }

        public Node Expression()
        {
            switch (TokenType)
            {
                case TokenType.IntValue:
                case TokenType.StringValue:
                case TokenType.BoolValue:
                case TokenType.Identifier:
                case TokenType.OpenParen:
                {
                    var opnd1 = Operand();
                    if (TokenType == TokenType.Operator)
                    {
                        var op = Operator();
                        var opnd2 = Operand();

                        return new BinaryNode
                        {
                            Left = opnd1,
                            Op = op,
                            Right = opnd2
                        };
                    }

                    return opnd1;
                }
                case TokenType.Not:
                {
                    MatchTokenType(TokenType.Not); // just eat it
                    // var op = UnaryOperator(); // TODO: this is redundant
                    var opnd = Operand();

                    return new UnaryNode
                    {
                        Expression = opnd
                    };
                }
                default:
                    UnexpectedTokenError(null);
                    break;
            }

            return null;
            // return Node.Of(NodeType.Unknown);
        }

        public Node Operand()
        {
            switch (TokenType)
            {
                case TokenType.IntValue:
                case TokenType.StringValue:
                case TokenType.BoolValue:
                {
                    var token = MatchTokenType(TokenType);
                    return new LiteralNode
                    {
                        Token = token,
                        Type = TokenToPrimitiveType.TryGetValueOrDefault(token.Type)
                    };
                }
                case TokenType.Identifier:
                {
                    var t = MatchTokenType(TokenType);
                    return new VariableNode
                    {
                        Token = t
                    };
                }
                case TokenType.OpenParen:
                    MatchTokenType(TokenType.OpenParen);
                    var n = Expression();
                    MatchTokenType(TokenType.CloseParen);
                    return n;
                default:
                    UnexpectedTokenError(null);
                    break;
            }

            return null; //Node.Of(NodeType.Unknown);
        }
        
        public static Dictionary<TokenType, PrimitiveType> TokenToPrimitiveType = new Dictionary<TokenType, PrimitiveType>()
        {
            [TokenType.IntValue] = PrimitiveType.Int,
            [TokenType.StringValue] = PrimitiveType.String,
            [TokenType.BoolValue] = PrimitiveType.Bool,
        };

        public Token Operator()
        {
            var t = MatchTokenType(TokenType.Operator);

            if (t.Type == TokenType.Unknown) UnexpectedTokenError(null);

            return t;
        }

        // public Node ValueOrIdentifier(TokenType tt)
        // {
        //     var t = MatchTokenType(tt);
        //
        //     switch (tt)
        //     {
        //         case TokenType.Identifier:
        //             return new IdentifierNode
        //             {
        //                 Name = t.Content
        //             };
        //         default:
        //             return new LiteralNode
        //             {
        //                 Value = t.Content,
        //                 ValueType = TokenToPrimitiveType.TryGetValueOrDefault(tt, PrimitiveType.Unknown)
        //             };
        //     }
        // }

        /*
        public Node UnaryOperator() // TODO: this was a bit wonky 
        {
            if (TokenType == TokenType.Not)
            {
                MatchTokenType(TokenType.Not);
                return Node.Of(NodeType.Not);
            }

            // TODO: needs to check if next one is a valid opnd follow
            return Node.Of(NodeType.Unknown);
        }
        */

        public Token MatchTokenType(TokenType tt)
        {
            var matchedToken = InputToken;
            if (TokenType == tt)
            {
                NextToken();
                Console.WriteLine("matched token {0}", tt);
                return matchedToken;
            }

            UnexpectedTokenError(tt);
            NextToken();
            return matchedToken;
        }

        public Token MatchOneOfTokenTypes(params TokenType[] tts)
        {
            foreach (var tt in tts)
                if (TokenType == tt)
                    return MatchTokenType(tt);
            UnexpectedTokenError(null); // TODO: one of
            return null;
        }

        public Token MatchKeywordType(KeywordType kwt)
        {
            if (TokenKeywordType == kwt)
            {
                var matchedToken = InputToken;
                NextToken();
                Console.WriteLine("matched keyword {0}", kwt);
                return matchedToken;
            }

            UnexpectedKeywordError(kwt);
            return null;
        }

        public Token MatchKeywordTypeList(KeywordType[] kwtl)
        {
            foreach (var kwt in kwtl)
                if (TokenKeywordType == kwt)
                {
                    var matchedToken = InputToken;
                    NextToken();
                    Console.WriteLine("matched keyword {0}", kwt);
                    return matchedToken;
                }

            UnexpectedKeywordError(kwtl);
            return null;
        }

        public void MatchContent(string s)
        {
            if (TokenContent.Equals(s)) return;
            ParseError($"expected {s}");
        }
    }
}