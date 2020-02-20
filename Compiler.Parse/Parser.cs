using System;
using System.Collections.Generic;
using System.Text;
using Compiler.Common;
using Compiler.Scan;
using NodeType = Compiler.Common.Node.NodeType;

namespace Parse
{
    public class Parser
    {
        private readonly Scanner scanner;
        private Token InputToken;
        private Node tree;
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
            tree = Node.Of(NodeType.Program);
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

        public void Program()
        {
            NextToken();


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
                    tree.AddChild(statements);
                    tree.AddChild(Node.Of(NodeType.EOF));
                    break;
                }
                case TokenType.Identifier:
                case TokenType.EOF:
                {
                    var statements = StatementList();
                    MatchTokenType(TokenType.EOF);
                    tree.AddChild(statements);
                    tree.AddChild(Node.Of(NodeType.EOF));
                    break;
                }
                default:
                    UnexpectedTokenError(null);
                    break;
            }

            Node.PrintTree(tree);
        }


        public void SkipToTokenType(TokenType tt)
        {
            while (TokenType != tt && TokenType != TokenType.EOF) NextToken();
            if (TokenType != TokenType.EOF) NextToken();
        }

        public Node StatementList()
        {
            var node = Node.Of(NodeType.StatementList);

            switch (TokenType)
            {
                case TokenType.Keyword when DoBlock && TokenKeywordType == KeywordType.End:
                    break;
                case TokenType.Keyword when StatementFirstKeywords.Includes(TokenKeywordType):
                    node.AddChild(Statement());
                    node.AddChild(StatementList());
                    break;
                case TokenType.Keyword:
                    UnexpectedKeywordError(StatementFirstKeywords);
                    SkipToTokenType(TokenType.Separator);
                    node.AddChild(Statement());
                    node.AddChild(StatementList());
                    break;
                case TokenType.Identifier:
                    node.AddChild(Statement());
                    node.AddChild(StatementList());
                    break;
                case TokenType.EOF:
                    break;
                default:
                    UnexpectedTokenError(null);
                    break;
            }

            return node;
        }

        public Node Statement()
        {
            var node = Node.Of(NodeType.Statement);

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
                            node = VarStatement();
                            break;
                        case KeywordType.For:
                            node = ForStatement();
                            break;
                        case KeywordType.Read:
                            node = ReadStatement();
                            break;
                        case KeywordType.Print:
                            node = PrintStatement();
                            break;
                        case KeywordType.Assert:
                            node = AssertStatement();
                            break;
                        case KeywordType.End when DoBlock:
                            break;
                        default:
                            UnexpectedKeywordError(expected);
                            SkipToTokenType(TokenType.Separator);
                            return node;
                        // break;
                    }

                    break;
                case TokenType.Identifier:
                    node = AssignmentStatement();
                    break;
                default:
                    UnexpectedTokenError(null);
                    break;
            }

            MatchTokenType(TokenType.Separator);

            return node;
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
            var id = ValueOrIdentifier(TokenType.Identifier);
            MatchTokenType(TokenType.Assignment);
            var expr = Expression();

            return Node.Of(NodeType.VariableAssignment, id, expr);
        }

        private Node AssertStatement()
        {
            NextToken();
            MatchTokenType(TokenType.OpenParen);
            var expr = Expression();
            MatchTokenType(TokenType.CloseParen);

            return Node.Of(NodeType.Assert, expr);
        }

        private Node PrintStatement()
        {
            NextToken();
            var value = Expression();

            return Node.Of(NodeType.Print, value);
        }

        private Node ReadStatement()
        {
            NextToken();
            var id = ValueOrIdentifier(TokenType.Identifier);

            return Node.Of(NodeType.Read, id);
        }

        private Node ForStatement()
        {
            NextToken();

            var id = ValueOrIdentifier(TokenType.Identifier);
            MatchKeywordType(KeywordType.In);
            var rangeStart = Expression();
            MatchTokenType(TokenType.Range);
            var rangeEnd = Expression();

            var statements = DoEndBlock(KeywordType.For);

            return Node.Of(NodeType.For, id, rangeStart, rangeEnd, statements);
        }

        public Node VarStatement()
        {
            var n = Node.Of(NodeType.VariableDeclaration);

            NextToken();
            var id = ValueOrIdentifier(TokenType.Identifier);
            MatchTokenType(TokenType.Colon);
            var type = Type();

            n.AddChild(id);
            n.AddChild(type);

            if (TokenType == TokenType.Separator) return n;

            MatchTokenType(TokenType.Assignment);
            var value = Expression();

            CheckType(value, type.Type);
            n.AddChild(value);

            return n;
        }

        public void CheckType(Node n, NodeType nt)
        {
            switch (n.Type)
            {
                case NodeType.ValueExpression:
                    CheckType(n.Children[0], nt);
                    break;
                case NodeType.BinaryExpression:
                    CheckType(n.Children[0], nt);
                    CheckType(n.Children[2], nt);
                    break;
                case NodeType.UnaryExpression:
                    CheckType(n.Children[1], nt);
                    break;
                default:
                    if (n.Type != Node.TypeToValue.TryGetValueOrDefault(nt)) TypeError(nt, n.Type);
                    break;
            }
        }

        public Node Type()
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
                return Node.Of(NodeType.Unknown);
            }

            // anytype should never happen after ^
            var nt = Node.KeywordToNodeType.TryGetValueOrDefault(tt.KeywordType, NodeType.AnyType);

            return Node.Of(nt);
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

                        return Node.Of(NodeType.BinaryExpression, opnd1, op, opnd2);
                    }

                    return Node.Of(NodeType.ValueExpression, opnd1);
                }
                case TokenType.Not:
                {
                    var op = UnaryOperator(); // TODO: this is redundant
                    var opnd = Operand();

                    return op != null
                        ? Node.Of(NodeType.UnaryExpression, op, opnd)
                        : Node.Of(NodeType.UnaryExpression, opnd);
                }
                default:
                    UnexpectedTokenError(null);
                    break;
            }

            return Node.Of(NodeType.Unknown);
        }

        public Node Operand()
        {
            switch (TokenType)
            {
                case TokenType.IntValue:
                case TokenType.StringValue:
                case TokenType.BoolValue:
                case TokenType.Identifier:
                    return ValueOrIdentifier(TokenType);
                case TokenType.OpenParen:
                    MatchTokenType(TokenType.OpenParen);
                    var n = Expression();
                    MatchTokenType(TokenType.CloseParen);
                    return n;
                default:
                    UnexpectedTokenError(null);
                    break;
            }

            return Node.Of(NodeType.Unknown);
        }


        public Node Operator()
        {
            var t = MatchTokenType(TokenType.Operator);
            var nt = Node.OperatorToNodeType.TryGetValueOrDefault(t.Content, NodeType.Unknown);

            if (nt != NodeType.Unknown) return Node.Of(nt);

            UnexpectedTokenError(null);
            return Node.Of(NodeType.Unknown);
        }

        public Node ValueOrIdentifier(TokenType tt)
        {
            var t = MatchTokenType(tt);

            var nt = Node.TokenToNodeType.TryGetValueOrDefault(tt, NodeType.Unknown);
            var n = Node.Of(nt);
            n.Value = t.Content;

            return n;
        }

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