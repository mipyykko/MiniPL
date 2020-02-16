using System;
using System.Collections.Generic;
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

        private KeywordType TokenKeywordType => InputToken.KeywordType;
        private TokenType TokenType => InputToken.Type;
        private string TokenContent => InputToken.Content;

        private readonly KeywordType[] StatementFirstKeywords = {
            KeywordType.Var,
            KeywordType.For,
            KeywordType.Read,
            KeywordType.Print,
            KeywordType.Assert
        };

        public Parser(Scanner scanner)
        {
            this.scanner = scanner;
            this.tree = Node.Of(NodeType.Program);
        }


        public void Parse()
        {
            Program();
        }

        public void ParseError(string error)
        {
            Console.WriteLine(error);
            while (TokenType != TokenType.Separator && TokenType != TokenType.EOF)
            {
                NextToken(); // gracious, eh
            }
            //throw new Exception(error);
        }

        private string GetLine => scanner.Text.Lines[InputToken.SourceInfo.lineRange.Item1];
        private string GetErrorPosition => new string(' ', InputToken.SourceInfo.lineRange.Item2 - 1)
            + new string('^', InputToken.SourceInfo.lineRange.Item3 - InputToken.SourceInfo.lineRange.Item2);

        public void UnexpectedKeywordError(KeywordType? kwt)
        {
            ParseError(String.Format("{0}\n{1}\nexpected keyword {2}, got {3}", GetLine, GetErrorPosition, TokenKeywordType, kwt));
        }

        public void UnexpectedTokenError(TokenType? tt)
        {
            ParseError(String.Format("{0}\n{1}\nexpected token {2}, got {3}", GetLine, GetErrorPosition, TokenType, tt));
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
                case TokenType.Keyword:
                    {
                        if (Array.IndexOf(StatementFirstKeywords, TokenKeywordType) < 0)
                        {
                            UnexpectedKeywordError(null);
                        }
                        Node statements = StatementList();
                        NextToken();
                        MatchTokenType(TokenType.EOF);
                        tree.AddChild(statements);
                        tree.AddChild(Node.Of(NodeType.EOF));
                        break;
                    }
                case TokenType.Identifier:
                case TokenType.EOF:
                    {
                        Node statements = StatementList();
                        MatchTokenType(TokenType.EOF);
                        tree.AddChild(statements);
                        tree.AddChild(Node.Of(NodeType.EOF));
                        break;
                    }
                default:
                    UnexpectedTokenError(null);
                    break;
            }
            PrintTree(tree, 0);
        }

        public void PrintTree(Node node, int depth)
        {
            if (node == null)
            {
                return;
            }

            Console.WriteLine($"{new string(' ', depth * 2)}{node}");

            foreach (Node child in node.Children)
            {
                PrintTree(child, depth + 1);
            }
        }

        public Node StatementList()
        {
            Node node = Node.Of(NodeType.StatementList);

            switch (TokenType)
            {
                case TokenType.Keyword:
                    if (Array.IndexOf(StatementFirstKeywords, TokenKeywordType) >= 0)
                    {
                        node.AddChild(Statement());
                        node.AddChild(StatementList());
                        break;
                    }
                    if (TokenKeywordType == KeywordType.End)
                    {
                        break;
                    }
                    UnexpectedKeywordError(null);
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
            Node node = Node.Of(NodeType.Statement);

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
                        default:
                            UnexpectedKeywordError(null);
                            break;
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
            MatchKeywordType(KeywordType.Do);
            Node statements = StatementList(); // expect End
            MatchKeywordType(KeywordType.End);
            MatchKeywordType(expectedEnd); // ie. started with for, end with "end for";

            return statements;
        }

        private Node AssignmentStatement()
        {
            Node id = ValueOrIdentifier(TokenType.Identifier);
            MatchTokenType(TokenType.Assignment);
            Node expr = Expression();

            return Node.Of(NodeType.VariableAssignment, id, expr);
        }

        private Node AssertStatement()
        {
            NextToken();
            MatchTokenType(TokenType.OpenParen);
            Node expr = Expression();
            MatchTokenType(TokenType.CloseParen);

            return Node.Of(NodeType.Assert, expr);
        }

        private Node PrintStatement()
        {
            NextToken();
            Node value = Expression();

            return Node.Of(NodeType.Print, value);
        }

        private Node ReadStatement()
        {
            NextToken();
            Node id = ValueOrIdentifier(TokenType.Identifier);

            return Node.Of(NodeType.Read, id);
        }

        private Node ForStatement()
        {
            NextToken();

            Node id = ValueOrIdentifier(TokenType.Identifier);
            MatchKeywordType(KeywordType.In);
            Node rangeStart = Expression();
            MatchTokenType(TokenType.Range);
            Node rangeEnd = Expression();

            Node statements = DoEndBlock(KeywordType.For);

            return Node.Of(NodeType.For, id, rangeStart, rangeEnd, statements);
        }

        public Node VarStatement()
        {
            Node n = Node.Of(NodeType.VariableDeclaration);

            NextToken();
            Node id = ValueOrIdentifier(TokenType.Identifier);
            MatchTokenType(TokenType.Colon);
            Node type = Type();
            // TODO: should peek and error

            n.AddChild(id);
            n.AddChild(type);

            if (TokenType != TokenType.Separator)
            {
                MatchTokenType(TokenType.Assignment);
                Node value = Expression();
                // n.Type = Node.NodeType.VariableAssignment;
                n.AddChild(value);
            }
            return n;
        }

        public Node Type()
        {
            Token tt = MatchKeywordTypeList(new KeywordType[] {
                KeywordType.Int,
                KeywordType.String,
                KeywordType.Bool
            });

            if (tt == null)
            {
                return null;
            }

            // anytype should never happen after ^
            NodeType nt = Node.KeywordToNodeType.TryGetValueOrDefault(tt.KeywordType, NodeType.AnyType);

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
                        Node opnd1 = Operand();
                        if (TokenType == TokenType.Operator)
                        {
                            Node op = Operator();
                            Node opnd2 = Operand();

                            return Node.Of(NodeType.BinaryExpression, opnd1, op, opnd2);
                        }
                        return Node.Of(NodeType.ValueExpression, opnd1);
                    }
                case TokenType.Not:
                    {
                        Node op = UnaryOperator();
                        Node opnd = Operand();

                        return op != null ? Node.Of(NodeType.UnaryExpression, op, opnd)
                                  : Node.Of(NodeType.UnaryExpression, opnd);
                    }
                default:
                    UnexpectedTokenError(null);
                    break;
            }
            return null;
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
                    Node n = Expression();
                    MatchTokenType(TokenType.CloseParen);
                    return n;
                default:
                    UnexpectedTokenError(null);
                    break;
            }
            return null;
        }


        public Node Operator()
        {
            Token t = MatchTokenType(TokenType.Operator);
            NodeType nt = Node.OperatorToNodeType.TryGetValueOrDefault(t.Content, NodeType.Unknown);

            if (nt != NodeType.Unknown)
            {
                return Node.Of(nt);
            }

            UnexpectedTokenError(null);
            return null;
        }

        public Node ValueOrIdentifier(TokenType tt)
        {
            Token t = MatchTokenType(tt);

            NodeType nt = Node.TokenToNodeType.TryGetValueOrDefault(tt, NodeType.Unknown);
            Node n = Node.Of(nt);
            n.Value = t.Content;

            return n;
        }

        public Node UnaryOperator()
        {
            if (TokenType == TokenType.Not)
            {
                MatchTokenType(TokenType.Not);
                return Node.Of(NodeType.Not);
            }
            // TODO: needs to check if next one is a valid opnd follow
            return null;
        }

        public Token MatchTokenType(TokenType tt)
        {
            if (TokenType == tt)
            {
                Token matchedToken = InputToken;
                NextToken();
                Console.WriteLine("matched token {0}", tt);
                return matchedToken;
            }
            UnexpectedTokenError(tt);
            return null;
        }

        public Token MatchKeywordType(KeywordType kwt)
        {
            if (TokenKeywordType == kwt)
            {
                Token matchedToken = InputToken;
                NextToken();
                Console.WriteLine("matched keyword {0}", kwt);
                return matchedToken;
            }
            UnexpectedKeywordError(kwt);
            return null;
        }

        public Token MatchKeywordTypeList(KeywordType[] kwtl)
        {
            foreach (KeywordType kwt in kwtl)
            {
                if (TokenKeywordType == kwt)
                {
                    Token matchedToken = InputToken;
                    NextToken();
                    Console.WriteLine("matched keyword {0}", kwt);
                    return matchedToken;
                }
            }
            UnexpectedKeywordError(null);
            return null;
        }

        public void MatchContent(string s)
        {
            if (TokenContent.Equals(s))
            {
                return;
            }
            ParseError(String.Format("expected {0}", s));
        }

    }
}
