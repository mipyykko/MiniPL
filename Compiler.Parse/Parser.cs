using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Compiler.Common;
using Compiler.Common.AST;
using Compiler.Common.Errors;
using Compiler.Scan;
using Node = Compiler.Common.AST.Node;
using StatementType = Compiler.Common.StatementType;
using static Compiler.Common.Util;
using static Compiler.Parse.Grammar;

namespace Compiler.Parse
{
    public class Parser
    {
        private Text Source => Context.Source;
        private IErrorService ErrorService => Context.ErrorService;
        private readonly Scanner _scanner;
        private Token _inputToken;

        // private Node tree;
        private bool DoBlock { get; set; }

        private KeywordType InputTokenKeywordType => _inputToken.KeywordType;
        private TokenType InputTokenType => _inputToken.Type;
        private string InputTokenContent => _inputToken.Content;

        private static readonly Node NoOpStatement = new NoOpNode();

        public Parser(Scanner scanner)
        {
            this._scanner = scanner;
        }

        private void ParseError(ErrorType type, Token errorToken, string message)
        {
            var sb = new StringBuilder($"{GetLine(errorToken)}\n{GetErrorPosition(errorToken)}\n");
            sb.Append(message);
            var errorMessage = sb.ToString();
            Console.WriteLine(errorMessage);
            ErrorService.Add(type, errorToken, errorMessage);
            
            throw new SyntaxErrorException(errorMessage);
        }

        private string GetLine(Token t) => Source.Lines[t.SourceInfo.LineRange.Line];

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
            var errorToken = _inputToken;
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
            var errorToken = _inputToken;
            SkipToTokenType(TokenType.Separator);
            ParseError(ErrorType.UnexpectedToken, errorToken, sb.ToString());
        }

        private void UnexpectedContentError(params string[] sl)
        {
            var sb = new StringBuilder();
            sb.Append(
                sl.Length switch
                {
                    0 => $"unexpected token {InputTokenContent} of type {InputTokenType}",
                    1 => $"expected {sl[0]}, got {InputTokenContent}",
                    _ => $"expected one of {string.Join(", ", sl)}, got {InputTokenContent}"
                }
            );
            var errorToken = _inputToken;
            SkipToTokenType(TokenType.Separator);
            ParseError(ErrorType.SyntaxError, errorToken, sb.ToString());
        }
        private void NextToken() => _inputToken = _scanner.GetNextToken();
        
        private KeywordType[] StatementFirstKeywords =>
            DoBlock
                ? DoBlockStatementFirstKeywords
                : DefaultStatementFirstKeywords;

        public Node Program()
        {
            NextToken();

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
        }

        private dynamic Match(object match)
        {
            try
            {
                return match switch
                {
                    _ when match is TokenType tt => MatchTokenType(tt),
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
                    StatementType.UnaryOperator => UnaryOperator(), // returns token
                    _ => ErrorService.Add(
                        ErrorType.InvalidOperation,
                        _inputToken,
                        $"tried to match unknown token ${match}")
                };
            }
            catch (SyntaxErrorException)
            {
                return match switch
                {
                    _ when match is TokenType => _inputToken,
                    _ when match is KeywordType => _inputToken,
                    _ when match is KeywordType[] => _inputToken,
                    StatementType.UnaryOperator => _inputToken,
                    StatementType.Type => PrimitiveType.Void,
                    _ => NoOpStatement
                };
            }
        }

        private object[] MatchSequence(params object[] seq)
        {
            return seq.Select(Match).ToArray();
        }

        private void SkipToTokenType(TokenType tt)
        {
            while (InputTokenType != tt && InputTokenType != TokenType.EOF) NextToken();
            // if (InputTokenType != TokenType.EOF) NextToken();
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
                case TokenType.Keyword when StatementFirstKeywords.Includes(InputTokenKeywordType):
                case TokenType.Identifier:
                    return (Node) Match(StatementType.StatementStatementList);
                case TokenType.Keyword:
                    UnexpectedKeywordError(StatementFirstKeywords);
                    return NoOpStatement;
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

                    if (InputTokenType != TokenType.Operator) return opnd1;

                    MatchSequence(
                        TokenType.Operator,
                        StatementType.Operand
                    ).Deconstruct(
                        out Token op,
                        out Node opnd2
                    );

                    return new ExpressionNode
                    {
                        Token = opnd1.Token,
                        Expression = new BinaryNode
                        {
                            Left = opnd1,
                            Token = op,
                            Right = opnd2
                        }
                    };
                }
                case TokenType.Operator:
                {
                    MatchSequence(
                        StatementType.UnaryOperator,
                        StatementType.Operand
                    ).Deconstruct(
                        out Token op,
                        out Node opnd
                    );

                    return new ExpressionNode
                    {
                        Type = op.Content switch
                        {
                            "!" => PrimitiveType.Bool,
                            _ => opnd.Type
                        },
                        Expression = new UnaryNode
                        {
                            Token = op,
                            Type = opnd.Type,
                            Value = opnd
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
                    var (_, n, __) = (
                        MatchTokenType(TokenType.OpenParen),
                        Expression(),
                        MatchTokenType(TokenType.CloseParen)
                    );
                    return n;
                case TokenType.Operator:
                    MatchSequence(
                        StatementType.UnaryOperator,
                        StatementType.Operand
                    ).Deconstruct(
                        out Token op,
                        out Node opnd
                    );
                    return new UnaryNode
                    {
                        Token = op,
                        Type = opnd.Type,
                        Value = opnd
                    };
                default:
                    UnexpectedTokenError(OperandFirstTokens);
                    break;
            }

            return NoOpStatement; //Node.Of(NodeType.Unknown);
        }

        private Token UnaryOperator() // TODO: this was a bit wonky 
        {
            if (MatchContent(UnaryOperators))
            {
                return MatchTokenType(TokenType.Operator);
            }

            return _inputToken; // TODO: hmm
        }

        private Token MatchTokenType(TokenType tt)
        {
            var matchedToken = _inputToken;
            if (InputTokenType == tt)
            {
                NextToken();
                return matchedToken;
            }

            UnexpectedTokenError(tt);
            return matchedToken;
        }

        private Token MatchKeywordType(KeywordType kwt)
        {
            var matchedToken = _inputToken;
            if (InputTokenKeywordType == kwt)
            {
                NextToken();
                return matchedToken;
            }

            UnexpectedKeywordError(kwt);
            return matchedToken;
        }

        private Token MatchKeywordType(KeywordType[] kwtl)
        {
            var matchedToken = _inputToken;
            if (kwtl.Any(kwt => InputTokenKeywordType == kwt))
            {
                NextToken();
                return matchedToken;
            }

            UnexpectedKeywordError(kwtl);
            return matchedToken;
        }

        private bool MatchContent(string s)
        {
            if (InputTokenContent.Equals(s)) return true;
            ParseError(ErrorType.SyntaxError, _inputToken, $"expected {s}, got {InputTokenContent}");
            return false;
        }

        private bool MatchContent(string[] sl)
        {
            if (sl.Any(InputTokenContent.Equals))
            {
                return true;
            }

            UnexpectedContentError(sl);
            return false;
        }
    }
}