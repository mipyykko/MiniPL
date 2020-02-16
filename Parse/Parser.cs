using System;
using System.Collections.Generic;
using Common;
using Compiler.Common;
using Compiler.Scan;
using Text = Compiler.Common.Text;

namespace Parse
{
    public class Parser
    {
        private readonly Scanner scanner;
        private Token inputToken;
        private Node tree;

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
            this.tree = Node.Of(Node.NodeType.Program);
        }


        public void Parse()
        {
            Program();
        }

        public void ParseError(string error)
        {
            throw new Exception(error);
        }

        public void UnexpectedKeywordError(KeywordType? kwt)
        {
            ParseError(String.Format("expected keyword {0}, got {1}", inputToken.KeywordType, kwt));
        }

        public void UnexpectedTokenError(TokenType? tt)
        {
            ParseError(String.Format("expected token {0}, got {1}", inputToken.Type, tt));
        }

        public void NextToken()
        {
            inputToken = scanner.GetNextToken();
        }

        public void Program()
        {
            NextToken();

            switch (inputToken.Type)
            {
                case TokenType.Keyword:
                    {
                        if (Array.IndexOf(StatementFirstKeywords, inputToken.KeywordType) < 0)
                        {
                            UnexpectedKeywordError(null);
                        }
                        Node statements = StatementList();
                        NextToken();
                        MatchTokenType(TokenType.EOF);
                        tree.AddChild(statements);
                        tree.AddChild(Node.Of(Node.NodeType.EOF));
                        break;
                    }
                case TokenType.Identifier:
                case TokenType.EOF:
                    {
                        Node statements = StatementList();
                        MatchTokenType(TokenType.EOF);
                        tree.AddChild(statements);
                        tree.AddChild(Node.Of(Node.NodeType.EOF));
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

            Console.WriteLine($"{new String(' ', depth * 2)}{node}");

            foreach (Node child in node.Children)
            {
                PrintTree(child, depth + 1);
            }
        }

        public Node StatementList() 
        {
            Node node = Node.Of(Node.NodeType.StatementList);

            switch (inputToken.Type)
            {
                case TokenType.Keyword:
                    if (Array.IndexOf(StatementFirstKeywords, inputToken.KeywordType) >= 0)
                    {
                        node.AddChild(Statement());
                        node.AddChild(StatementList());
                        break;
                    }
                    if (inputToken.KeywordType == KeywordType.End)
                    {
                        break; // TODO: doesn't actually check if we're in a for loop
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
            Node node = Node.Of(Node.NodeType.Statement);

            switch (inputToken.Type) 
            {
                case TokenType.Keyword:
                    switch (inputToken.KeywordType)
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

        private Node AssignmentStatement()
        {
            Node id = ValueOrIdentifier(TokenType.Identifier);
            MatchTokenType(TokenType.Assignment);
            Node expr = Expression();

            return Node.Of(Node.NodeType.VariableAssignment, id, expr);
        }

        private Node AssertStatement()
        {
            NextToken();
            MatchTokenType(TokenType.OpenParen);
            Node expr = Expression();
            MatchTokenType(TokenType.CloseParen);

            return Node.Of(Node.NodeType.Assert, expr);
        }

        private Node PrintStatement()
        {
            NextToken();
            Node value = Expression();

            return Node.Of(Node.NodeType.Print, value);
        }

        private Node ReadStatement()
        {
            NextToken();
            Node id = ValueOrIdentifier(TokenType.Identifier);

            return Node.Of(Node.NodeType.Read, id);
        }

        private Node ForStatement()
        {
            NextToken();

            Node id = ValueOrIdentifier(TokenType.Identifier);
            MatchKeywordType(KeywordType.In);
            Node rangeStart = Expression();
            MatchTokenType(TokenType.Range);
            Node rangeEnd = Expression();
            MatchKeywordType(KeywordType.Do);

            Node statements = StatementList();
            MatchKeywordType(KeywordType.End);
            MatchKeywordType(KeywordType.For);

            return Node.Of(Node.NodeType.For, id, rangeStart, rangeEnd, statements);
        }

        public Node VarStatement()
        {
            Node n = Node.Of(Node.NodeType.VariableDeclaration);

            NextToken();
            Node id = ValueOrIdentifier(TokenType.Identifier);
            MatchTokenType(TokenType.Colon);
            Node type = Type();
            // TODO: should peek and error

            n.AddChild(id);
            n.AddChild(type);

            if (inputToken.Type != TokenType.Separator)
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

            Node.KeywordToNodeType.TryGetValue(tt.KeywordType, out Node.NodeType nt);

            return Node.Of(nt);
        }

        public Node Expression()
        {
            switch (inputToken.Type)
            {
                case TokenType.IntValue:
                case TokenType.StringValue:
                case TokenType.BoolValue:
                case TokenType.Identifier:
                case TokenType.OpenParen:
                    {
                        Node opnd1 = Operand();
                        if (inputToken.Type == TokenType.Operator)
                        {
                            Node op = Operator();
                            Node opnd2 = Operand();

                            return Node.Of(Node.NodeType.BinaryExpression, opnd1, op, opnd2);
                        }
                        return Node.Of(Node.NodeType.ValueExpression, opnd1);
                    }
                case TokenType.Not:
                    {
                        Node op = UnaryOperator();
                        Node opnd = Operand();

                        return op != null ? Node.Of(Node.NodeType.UnaryExpression, op, opnd)
                                  : Node.Of(Node.NodeType.UnaryExpression, opnd);
                    }
                default:
                    UnexpectedTokenError(null);
                    break;
            }
            return null;
        }

        public Node Operand()
        {
            switch (inputToken.Type)
            {
                case TokenType.IntValue:
                case TokenType.StringValue:
                case TokenType.BoolValue:
                case TokenType.Identifier:
                    return ValueOrIdentifier(inputToken.Type);
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

            switch (t.Content)
            {
                case "*":
                    return Node.Of(Node.NodeType.Multiplication);
                case "/":
                    return Node.Of(Node.NodeType.Division);
                case "+":
                    return Node.Of(Node.NodeType.Addition);
                case "-":
                    return Node.Of(Node.NodeType.Subtraction);
                case "&":
                    return Node.Of(Node.NodeType.And);
                case "=":
                    return Node.Of(Node.NodeType.Equals);
                case "<":
                    return Node.Of(Node.NodeType.LessThan);
                default:
                    UnexpectedTokenError(null);
                    break;
            }
            return null;
        }

        public Node ValueOrIdentifier(TokenType tt)
        {
            Token t = MatchTokenType(tt);

            Node.TokenToNodeType.TryGetValue(tt, out Node.NodeType nt);
            Node n = Node.Of(nt);
            n.Value = t.Content;

            return n;
        }

        public Node UnaryOperator()
        {
            if (inputToken.Type == TokenType.Not)
            {
                MatchTokenType(TokenType.Not);
                return Node.Of(Node.NodeType.Not);
            }
            // TODO: needs to check if next one is a valid opnd follow
            return null;
        }

        public Token MatchTokenType(TokenType tt)
        {
            if (inputToken.Type == tt)
            {
                Token matchedToken = inputToken;
                NextToken();
                Console.WriteLine("matched token {0}", tt);
                return matchedToken;
            }
            UnexpectedTokenError(tt);
            return null;
        }

        public Token MatchKeywordType(KeywordType kwt)
        {
            if (inputToken.KeywordType == kwt)
            {
                Token matchedToken = inputToken;
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
                if (inputToken.KeywordType == kwt)
                {
                    Token matchedToken = inputToken;
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
            if (inputToken.Content.Equals(s))
            {
                return;
            }
            ParseError(String.Format("expected {0}", s));
        }

    }
}
