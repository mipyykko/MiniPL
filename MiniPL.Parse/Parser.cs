using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using MiniPL.Common;
using MiniPL.Common.AST;
using MiniPL.Common.Errors;
using MiniPL.Scan;
using Node = MiniPL.Common.AST.Node;
using StatementType = MiniPL.Common.StatementType;
using static MiniPL.Common.Util;
using static MiniPL.Parse.Grammar;

namespace MiniPL.Parse
{
    public class Parser
    {
        private Text Source => Context.Source;
        private IErrorService ErrorService => Context.ErrorService;
        private readonly Scanner _scanner;
        private Token _inputToken;

        private bool DoBlock { get; set; }

        private void NextToken() => _inputToken = _scanner.GetNextToken();
        private KeywordType InputTokenKeywordType => _inputToken.KeywordType;
        private TokenType InputTokenType => _inputToken.Type;
        private string InputTokenContent => _inputToken.Content;
        private string GetLine(Token t) => Source.Lines[t.SourceInfo.LineRange.Line];

        private string GetErrorPosition(Token t) => new string(' ', Math.Max(0, t.SourceInfo.LineRange.Start))
                                                    + new string('^', Math.Max(1, t.Content.Length));

        private static readonly Node NoOpStatement = new NoOpNode();

        public Parser(Scanner scanner)
        {
            this._scanner = scanner;
        }

        #region ErrorHandling

        private void ParseError(ErrorType type, Token errorToken, string message)
        {
            var errorMessage =
                $"{GetLine(errorToken)}\n{GetErrorPosition(errorToken)}\n{message}";
            ErrorService.Add(type, errorToken, errorMessage);

            throw new SyntaxErrorException(errorMessage);
        }

        private void UnexpectedError<T>(ErrorType type, IReadOnlyList<T> items)
        {
            Console.WriteLine($"got error of type {type}");
            var message = type switch
            {
                ErrorType.UnexpectedKeyword => items.Count switch
                {
                    0 => $"unexpected keyword {InputTokenContent} of type {InputTokenKeywordType}",
                    1 =>
                    $"expected keyword of type {items[0]}, got {InputTokenContent} of type {InputTokenKeywordType}",
                    _ =>
                    $"expected one of keyword types {string.Join(", ", items)}, got {InputTokenContent} of type {InputTokenKeywordType}"
                },
                ErrorType.UnexpectedToken => items.Count switch
                {
                    0 => $"unexpected token {InputTokenContent} of type {InputTokenType}",
                    1 => $"expected token of type {items[0]}, got {InputTokenContent} of type {InputTokenType}",
                    _ =>
                    $"expected one of token types {string.Join(", ", items)}, got {InputTokenContent} of type {InputTokenType}"
                },
                ErrorType.SyntaxError => items.Count switch
                {
                    0 => $"unexpected {InputTokenContent} of type {InputTokenType}",
                    1 => $"expected {items[0]}, got {InputTokenContent}",
                    _ => $"expected one of {string.Join(", ", items)}, got {InputTokenContent}"
                },
                _ => ""
            };
            var errorToken = _inputToken;
            if (items.GetType() == typeof(TokenType[]) && items.Any(i =>
                (TokenType) (object) i == TokenType.Separator
            ))
            {
                //SkipToTokenType(TokenType.Separator, false);
                //NextToken();
            } else {
                NextToken();
                SkipToTokenType(StatementFirstTokens /*TokenType.Separator*/);
            }
            ParseError(type, errorToken, message);
        }

        private void UnexpectedKeywordError(params KeywordType[] kwts) =>
            UnexpectedError(ErrorType.UnexpectedKeyword, kwts);

        private void UnexpectedTokenError(params TokenType[] tts) =>
            UnexpectedError(ErrorType.UnexpectedToken, tts);

        private void UnexpectedOperatorError(params OperatorType[] ops) =>
            UnexpectedError(ErrorType.InvalidOperation, ops);
        
        private void UnexpectedContentError(params string[] sl) =>
            UnexpectedError(ErrorType.SyntaxError, sl);

        private void SkipToTokenType(params TokenType[] tts)
        {
            while (!tts.Includes(InputTokenType) && InputTokenType != TokenType.EOF) NextToken();
            // while (InputTokenType == tt && InputTokenType != TokenType.EOF) NextToken();
        }
        #endregion

        #region Matching

        private dynamic Match(dynamic match)
        {
            try
            {
                return match switch
                {
                    _ when match is string op => MatchContent(op), 
                    _ when match is string[] ops => MatchContent(ops), 
                    _ when match is TokenType tt => MatchTokenType(tt),
                    _ when match is TokenType[] tts => MatchTokenType(tts),
                    _ when match is KeywordType kwt => MatchKeywordType(kwt),
                    _ when match is KeywordType[] kwts => MatchKeywordType(kwts),
                    StatementType.Statement => Statement(),
                    StatementType.StatementList => StatementList(),
                    StatementType.StatementStatementList => StatementStatementList(),
                    StatementType.DoEndBlock => DoEndBlock(),
                    StatementType.AssignmentStatement => AssignmentStatement(),
                    StatementType.AssertStatement => AssertStatement(),
                    StatementType.PrintStatement => PrintStatement(),
                    StatementType.ReadStatement => ReadStatement(),
                    StatementType.ForStatement => ForStatement(),
                    StatementType.VarStatement => VarStatement(),
                    StatementType.NoOpStatement => NoOpStatement,
                    StatementType.Type => Type(),
                    StatementType.Expression => Expression(),
                    StatementType.Operand => Operand(),
                    _ => ErrorService.Add(
                        ErrorType.InvalidOperation,
                        _inputToken,
                        $"tried to match unknown token {match}")
                };
            }
            catch (SyntaxErrorException)
            {
                var error = true;
                dynamic result = null;
                while (error)
                {
                    try
                    {
                        error = false;
                        result = match switch
                        {
                            _ when match is TokenType => _inputToken,
                            _ when match is TokenType[] => _inputToken,
                            _ when match is KeywordType => _inputToken,
                            _ when match is KeywordType[] => _inputToken,
                            _ when match is string => _inputToken,
                            _ when match is string[] => _inputToken,
                            StatementType.Type => PrimitiveType.Void,
                            StatementType.StatementList => StatementList(),
                            StatementType.StatementStatementList => StatementStatementList(),
                            StatementType.Statement => Statement(),
                            _ => NoOpStatement
                        };
                    }
                    catch (SyntaxErrorException)
                    {
                        error = true;
                    }
                }

                return result;
            }
        }

        private dynamic[] MatchSequence(params dynamic[] seq)
        {
            return seq.Select(Match).ToArray();
        }

        private Token MatchTokenType(TokenType[] ttl, bool advance = true)
        {
            var matchedToken = _inputToken;
            if (ttl.Any(tt => InputTokenType == tt))
            {
                if (advance) NextToken();
                return matchedToken;
            }

            UnexpectedTokenError(ttl);
            return matchedToken;
        }

        private Token MatchTokenType(TokenType tt, bool advance = true) =>
            MatchTokenType(new[] {tt}, advance);

        private Token MatchKeywordType(KeywordType[] kwtl, bool advance = true)
        {
            var matchedToken = _inputToken;
            if (kwtl.Any(kwt => InputTokenKeywordType == kwt))
            {
                if (advance) NextToken();
                return matchedToken;
            }

            UnexpectedKeywordError(kwtl);
            return matchedToken;
        }

        private Token MatchKeywordType(KeywordType kwt, bool advance = true) =>
            MatchKeywordType(new [] {kwt}, advance);

        private Token MatchOperatorType(OperatorType[] ops, bool advance = true)
        {
            var matchedToken = _inputToken;
            var operatorType = Token.OperatorToOperatorType.TryGetValueOrDefault(InputTokenContent);
            if (ops.Any(op => operatorType == op))
            {
                if (advance) NextToken();
                return matchedToken;
            }

            UnexpectedOperatorError(ops);
            return matchedToken;
        }
        
        private Token MatchOperatorType(OperatorType op, bool advance = true) =>
            MatchOperatorType(new[] {op}, advance);

        private Token MatchContent(string s, bool advance = true)
        {
            var matchedToken = _inputToken;
            if (InputTokenContent.Equals(s))
            {
                if (advance) NextToken();
                return matchedToken;
            }

            UnexpectedContentError(s);
            return matchedToken;
        }

        private Token MatchContent(string[] sl, bool advance = true)
        {
            var matchedToken = _inputToken;
            if (sl.Any(InputTokenContent.Equals))
            {
                if (advance) NextToken();
                return matchedToken;
            }

            UnexpectedContentError(sl);
            return matchedToken;
        }

        private PrimitiveType Type()
        {
            var tt = MatchKeywordType(ExpectedTypes);
            if (tt != null) return tt.KeywordType.ToPrimitiveType();

            return PrimitiveType.Void;
        }

        #endregion Matching

        public Node Program()
        {
            NextToken();

            return StatementStatementList();
            /*
            MatchSequence(
                StatementType.StatementList,
                TokenType.EOF
            ).Deconstruct(
                out Node statements,
                out Token _
            );
            return new StatementListNode
            {
                Left = statements,
                Right = NoOpStatement
            };
        */
        }

        private Node Statement()
        {
            switch (InputTokenType)
            {
                case TokenType.Keyword:
                {
                    var statementType = InputTokenKeywordType switch
                    {
                        KeywordType.Var => StatementType.VarStatement,
                        KeywordType.For => StatementType.ForStatement,
                        KeywordType.Read => StatementType.ReadStatement,
                        KeywordType.Print => StatementType.PrintStatement,
                        KeywordType.Assert => StatementType.AssertStatement,
                        KeywordType.End when DoBlock => StatementType.NoOpStatement,
                        _ => StatementType.Error
                    };

                    if (statementType == StatementType.Error)
                    {
                        UnexpectedKeywordError(StatementFirstKeywords(DoBlock));
                    }
                    MatchSequence(
                        statementType,
                        TokenType.Separator
                    ).Deconstruct(out Node statement, out Token _);

                    return statement;
                }
                case TokenType.Identifier:
                {
                    MatchSequence(
                        StatementType.AssignmentStatement,
                        TokenType.Separator
                    ).Deconstruct(out Node statement, out Token _);

                    return statement;
                }
                default:
                    UnexpectedTokenError(StatementFirstTokens);
                    break;
            }

            return NoOpStatement;
        }

        private Node StatementList()
        {
            switch (InputTokenType)
            {
                case TokenType.Keyword when DoBlock && InputTokenKeywordType == KeywordType.End:
                case TokenType.EOF:
                    return NoOpStatement;
                case TokenType.Keyword when StatementFirstKeywords(DoBlock).Includes(InputTokenKeywordType):
                case TokenType.Identifier:
                    return (Node) Match(StatementType.StatementStatementList);
                case TokenType.Keyword:
                    UnexpectedKeywordError(StatementFirstKeywords(DoBlock));
                    return NoOpStatement;
                /*case TokenType.Separator:
                    // I guess we're recovering from an error
                    MatchTokenType(TokenType.Separator);
                    return StatementList();*/
                default:
                    UnexpectedTokenError(StatementFirstTokens);
                    return NoOpStatement;
            }
        }

        private Node StatementStatementList()
        {
            MatchSequence(
                StatementType.Statement,
                StatementType.StatementList
            ).Deconstruct(
                out Node left,
                out Node right
            );
            return new StatementListNode
            {
                Left = left,
                Right = right
            };
        }

        private Node DoEndBlock()
        {
            DoBlock = true;

            MatchSequence(
                KeywordType.Do,
                StatementType.StatementList,
                KeywordType.End
            ).Deconstruct(
                out Token _,
                out Node statements,
                out Token __
            );

            DoBlock = false;

            return statements;
        }

        private Node AssignmentStatement()
        {
            MatchSequence(
                TokenType.Identifier,
                TokenType.Assignment,
                StatementType.Expression
            ).Deconstruct(
                out Token id,
                out Token token,
                out Node expr
            );

            return new AssignmentNode
            {
                Token = token,
                Id = new VariableNode
                {
                    Token = id
                },
                Expression = expr,
            };
        }

        private Node AssertStatement()
        {
            MatchSequence(
                KeywordType.Assert,
                TokenType.OpenParen,
                StatementType.Expression,
                TokenType.CloseParen
            ).Deconstruct(
                out Token token,
                out Token _,
                out Node expr,
                out Token __);

            return new StatementNode
            {
                Token = token,
                Arguments = new List<Node> {expr}
            };
        }

        private Node PrintStatement()
        {
            MatchSequence(
                KeywordType.Print,
                StatementType.Expression
            ).Deconstruct(
                out Token token,
                out Node value
            );

            return new StatementNode
            {
                Token = token,
                Arguments = new List<Node> {value}
            };
        }

        private Node ReadStatement()
        {
            MatchSequence(
                KeywordType.Read,
                TokenType.Identifier
            ).Deconstruct(out Token token, out Token id);

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
            MatchSequence(
                KeywordType.For,
                TokenType.Identifier,
                KeywordType.In,
                StatementType.Expression,
                TokenType.Range,
                StatementType.Expression,
                StatementType.DoEndBlock,
                KeywordType.For
            ).Deconstruct(
                out Token token,
                out Token id,
                out Token __,
                out Node rangeStart,
                out Token ___,
                out Node rangeEnd,
                out Node statements,
                out Token ____
            );

            return new ForNode
            {
                Token = token,
                Id = new VariableNode
                {
                    Token = id
                },
                RangeStart = rangeStart,
                RangeEnd = rangeEnd,
                Statements = statements
            };
        }

        private Node VarStatement()
        {
            MatchSequence(
                KeywordType.Var,
                TokenType.Identifier,
                TokenType.Colon,
                StatementType.Type
            ).Deconstruct(
                out Token token,
                out Token id,
                out Token _,
                out PrimitiveType type
            );

            var n = new AssignmentNode
            {
                Token = token,
                Type = type,
                Id = new VariableNode
                {
                    Token = id,
                    Type = type
                }
            };
            
            MatchTokenType(new TokenType[] {TokenType.Assignment, TokenType.Separator}, false);

            if (InputTokenType == TokenType.Separator)
            {
                n.Expression = NoOpStatement;
                return n;
            }

            MatchSequence(
                TokenType.Assignment,
                StatementType.Expression
            ).Deconstruct(
                out Token _,
                out Node value
            );

            n.Expression = value;
            return n;
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
                    var operand1 = Operand();

                    if (InputTokenType != TokenType.Operator) return operand1;

                    MatchSequence(
                        BinaryOperators,
                        StatementType.Operand
                    ).Deconstruct(
                        out Token op,
                        out Node operand2
                    );

                    return new ExpressionNode
                    {
                        Token = operand1.Token,
                        Expression = new BinaryNode
                        {
                            Left = operand1,
                            Token = op,
                            Right = operand2
                        }
                    };
                }
                case TokenType.Operator:
                {
                    MatchSequence(
                        UnaryOperators,
                        StatementType.Operand
                    ).Deconstruct(
                        out Token op,
                        out Node operand
                    );

                    return new ExpressionNode
                    {
                        Token = op, // hmm
                        Type = op.Content switch
                        {
                            "!" => PrimitiveType.Bool,
                            _ => operand.Type
                        },
                        Expression = new UnaryNode
                        {
                            Token = op,
                            Type = operand.Type,
                            Value = operand
                        }
                    };
                }
                default:
                    UnexpectedTokenError(ExpressionFirstTokens);
                    break;
            }

            return NoOpStatement;
        }


        private Node Operand()
        {
            switch (InputTokenType)
            {
                case TokenType.IntValue:
                case TokenType.StringValue:
                case TokenType.BoolValue:
                {
                    var type = InputTokenType;
                    var token = MatchTokenType(type);
                    return new LiteralNode
                    {
                        Token = token,
                        Type = TokenToPrimitiveType(type)
                    };
                }
                case TokenType.Identifier:
                {
                    var token = MatchTokenType(TokenType.Identifier);
                    return new VariableNode
                    {
                        Token = token
                    };
                }
                case TokenType.OpenParen:
                    var (_, node, __) = (
                        MatchTokenType(TokenType.OpenParen),
                        Expression(),
                        MatchTokenType(TokenType.CloseParen)
                    );
                    return node;
                case TokenType.Operator:
                    MatchSequence(
                        UnaryOperators,
                        StatementType.Operand
                    ).Deconstruct(
                        out Token op,
                        out Node operand
                    );
                    return new UnaryNode
                    {
                        Token = op,
                        Type = operand.Type,
                        Value = operand
                    };
                default:
                    UnexpectedTokenError(OperandFirstTokens);
                    break;
            }

            return NoOpStatement;
        }
    }
}