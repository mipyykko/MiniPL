using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Compiler.Common;
using Compiler.Common.AST;
using Compiler.Symbols;
using Compiler.Scan;
using Node = Compiler.Common.AST.Node;

namespace Compiler.Parse
{
    public partial class Parser
    {
        private List<Error> errors = new List<Error>();
        private readonly Scanner scanner;
        private Token InputToken;

        private SymbolTable _symbolTable;

        // private Node tree;
        private bool DoBlock { get; set; }

        private KeywordType InputTokenKeywordType => InputToken.KeywordType;
        private TokenType InputTokenType => InputToken.Type;
        private string InputTokenContent => InputToken.Content;

        private static readonly Node NoOpStatement = new NoOpNode();

        public Parser(Scanner scanner, SymbolTable symbolTable)
        {
            /* TODO:
               get rid of those tuple things, or find some other thing
               to get out of that statement function after some of the parts
               fails - now it leads to cascading errors
            */
            this.scanner = scanner;
            _symbolTable = symbolTable;
        }

        public List<Error> Errors => errors;

        private void ParseError(ErrorType type, Token errorToken, string message)
        {
            var sb = new StringBuilder($"{GetLine(errorToken)}\n{GetErrorPosition(errorToken)}\n");
            sb.Append(message);
            var errorMessage = sb.ToString();
            Console.WriteLine(errorMessage);
            errors.Add(Error.Of(type, errorToken, errorMessage));
            throw new SyntaxErrorException(errorMessage);
        }

        private string GetLine(Token t) => scanner.Text.Lines[t.SourceInfo.LineRange.Line];

        private string GetErrorPosition(Token t) => new string(' ', Math.Max(0, t.SourceInfo.LineRange.Start))
                                           + new string('^', Math.Max(1, t.Content.Length));

        private void UnexpectedKeywordError(params KeywordType[] kwts)
        {
            var sb = new StringBuilder();
            sb.Append(
                kwts.Length switch
                {
                    0 => $"unexpected keyword {InputTokenContent} of type {InputTokenKeywordType}",
                    1 => $"expected keyword of type {kwts[0]}, got {InputTokenContent} of type {InputTokenKeywordType}",
                    _ =>
                    $"expected one of keyword types {string.Join(", ", kwts)}, got {InputTokenContent} of type {InputTokenKeywordType}"
                }
            );
            var errorToken = InputToken;
            SkipToTokenType(TokenType.Separator);
            ParseError(ErrorType.UnexpectedKeyword, errorToken, sb.ToString());
        }

        private void UnexpectedTokenError(params TokenType[] tts)
        {
            var sb = new StringBuilder();
            sb.Append(
                tts.Length switch
                {
                    0 => $"unexpected token {InputTokenContent} of type {InputTokenType}",
                    1 => $"expected token of type {tts[0]}, got {InputTokenContent} of type {InputTokenType}",
                    _ =>
                    $"expected one of token types {string.Join(", ", tts)}, got {InputTokenContent} of type {InputTokenType}"
                }
            );
            var errorToken = InputToken;
            SkipToTokenType(TokenType.Separator);
            ParseError(ErrorType.UnexpectedToken, errorToken, sb.ToString());
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
                            KeywordType.End when DoBlock => NoOpStatement,
                            _ => throw new SyntaxErrorException()
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
            catch (SyntaxErrorException)
            {
                return null;
            }
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

            CheckSymbol(id.Content, true);

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

            CheckSymbol(id.Content, true);

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

        private void CheckSymbol(string id, bool exists = false)
        {
            if (exists && _symbolTable.SymbolExists(id)) return;
            if (!exists && !_symbolTable.SymbolExists(id)) return;

            var errorToken = InputToken;
            SkipToTokenType(TokenType.Separator);

            if (exists)
            {
                ParseError(ErrorType.UndeclaredVariable,
                    errorToken,
                    $"variable {id} not declared");
            }
            else
            {
                ParseError(ErrorType.RedeclaredVariable,
                    errorToken,
                    $"variable {id} already declared");
            }
        }

        private Node ForStatement()
        {
            MatchKeywordType(KeywordType.For);
            var id = MatchTokenType(TokenType.Identifier);

            CheckSymbol(id.Content, true);

            MatchKeywordType(KeywordType.In);
            var rangeStart = Expression();
            MatchTokenType(TokenType.Range);
            var rangeEnd = Expression();

            // _symbolTable.SetControlVariable(id.Content);
            // TODO: don't know if parser should care about this

            var statements = DoEndBlock(KeywordType.For);

            // _symbolTable.UnsetControlVariable(id.Content); 

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
            try
            {
                var token = MatchKeywordType(KeywordType.Var);
                var id = MatchTokenType(TokenType.Identifier);

                CheckSymbol(id.Content, false);

                MatchTokenType(TokenType.Colon);
                var type = Type();

                _symbolTable.DeclareSymbol(id.Content, type); // TODO: error

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
            catch (SyntaxErrorException)
            {
                return null;
            }
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
                    var t = MatchTokenType(TokenType.Identifier);
                    CheckSymbol(t.Content, true);

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
            ParseError(ErrorType.SyntaxError, InputToken, $"expected {s}");
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