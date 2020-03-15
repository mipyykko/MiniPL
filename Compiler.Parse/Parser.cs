using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Compiler.Common;
using Compiler.Common.AST;
using Compiler.Symbols;
using Compiler.Scan;
using Node = Compiler.Common.AST.Node;
using StatementType = Compiler.Common.StatementType;
using static Compiler.Common.Util;
using static Compiler.Parse.Grammar;

namespace Compiler.Parse
{
    public class Parser
    {
        private List<Error> errors = new List<Error>();
        private readonly Scanner scanner;
        private Token InputToken;

        // private Node tree;
        private bool DoBlock { get; set; }

        private KeywordType InputTokenKeywordType => InputToken.KeywordType;
        private TokenType InputTokenType => InputToken.Type;
        private string InputTokenContent => InputToken.Content;

        private static readonly Node NoOpStatement = new NoOpNode();

        public Parser(Scanner scanner)
        {
            this.scanner = scanner;
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
            Console.WriteLine($"I expected {tts}");
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
                Right = new NoOpNode()
            };
        }

        private object Match(object match)
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
                        _ => throw new Exception($"what? {match}")
                    };
                }
                catch (SyntaxErrorException)
                {
                    Console.WriteLine($"throwing with {match}"); //  - seq was {string.Join(", ", seq)}; matches so far {string.Join(", ", matches)}");
                    return match switch
                    {
                        _ when match is TokenType => InputToken,
                        _ when match is KeywordType => InputToken,
                        _ when match is KeywordType[] => InputToken,
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

                    if (statementType == StatementType.Error)
                    {
                        UnexpectedKeywordError(StatementFirstKeywords);
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
                case TokenType.Keyword when StatementFirstKeywords.Includes(InputTokenKeywordType):
                case TokenType.Identifier:
                    return (Node) Match(StatementType.StatementStatementList);
                default:
                {
                    UnexpectedKeywordError(StatementFirstKeywords);
                    return NoOpStatement;
                }
            }
        }

        private Node StatementStatementList()
        {
            MatchSequence(
                StatementType.Statement,
                StatementType.StatementList
            ).Deconstruct(
                out Node left,
                // out Token _,
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
                out Token _,
                out Node expr
            );

            /*CheckSymbol(id.Content, true);
            var type = _symbolTable.LookupType(id.Content);
            
            if (expr.Type != type)
            {
                ParseError(ErrorType.TypeError,
                    expr.Token,
                    $"type error: expected value of type {type}, got {expr.Type}"
                );
                return NoOpStatement;
            }*/

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

            /*CheckSymbol(id.Content, true);
            var type = (PrimitiveType)_symbolTable.LookupType(id.Content);
            _symbolTable.UpdateSymbol(id.Content, DefaultValue(type));
                
            if (CheckSymbol(id.Content, true))
            {*/

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
            /*}

            // this is the error path
            return (Node) Match(
                StatementType.Statement
            );*/

        }

        /*private bool CheckSymbol(string id, bool exists = false)
        {
            if (exists && _symbolTable.SymbolExists(id)) return true;
            if (!exists && !_symbolTable.SymbolExists(id)) return true;

            var errorToken = InputToken;
            // SkipToTokenType(TokenType.Separator);

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

            return false;
        }*/

        private Node ForStatement()
        {
            MatchSequence(
                KeywordType.For,
                TokenType.Identifier,
                KeywordType.In,
                StatementType.Expression,
                TokenType.Range,
                StatementType.Expression
            ).Deconstruct(
                out Token _,
                out Token id,
                out Token __,
                out Node rangeStart,
                out Token ___,
                out Node rangeEnd
            );
            /*CheckSymbol(id.Content, true);

            var type = (PrimitiveType) _symbolTable.LookupType(id.Content);
            _symbolTable.SetControlVariable(id.Content);
            // TODO: update to initial literal or this
            _symbolTable.UpdateControlVariable(id.Content, type switch
            {
                PrimitiveType.Bool => "false",
                PrimitiveType.Int => "0",
                PrimitiveType.String => "",
                _ => null
            });*/

            MatchSequence(
                StatementType.DoEndBlock,
                KeywordType.For
            ).Deconstruct(
                out Node statements,
                out Token _
            );

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

            /*
            CheckSymbol(id.Content, false);
            _symbolTable.DeclareSymbol(id.Content, type); // TODO: error
            */

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

            MatchSequence(
                TokenType.Assignment,
                StatementType.Expression
            ).Deconstruct(
                out Token _,
                out Node value
            );

            if (value.Type != type)
            {
                ParseError(ErrorType.TypeError,
                    value.Token,
                    $"type error: expected value of type {type}, got {value.Type}"
                );
                return n;
            }
            
            /*
            // TODO: get literal value or default - this to elsewhere
            _symbolTable.UpdateSymbol(id.Content, type switch
            {
                PrimitiveType.Bool => "false",
                PrimitiveType.Int => 0,
                PrimitiveType.String => "",
                _ => null
            }); // TODO: error
            */
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
                        Type = op.Content switch
                        {
                            "<" => PrimitiveType.Bool,
                            "&" => PrimitiveType.Bool,
                            "=" => PrimitiveType.Bool,
                            _ => opnd1.Type
                        },
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
                    var token = MatchTokenType(InputTokenType);
                    return new LiteralNode
                    {
                        Token = token
                    };
                }
                case TokenType.Identifier:
                {
                    var token = MatchTokenType(TokenType.Identifier);
                    /*CheckSymbol(token.Content, true);
                    Console.WriteLine(_symbolTable.LookupSymbol(token.Content));
                    var (type, value) = _symbolTable.LookupSymbol(token.Content);
                    if (value == null)
                    {
                        ParseError(ErrorType.UninitializedVariable,
                            token,
                            $"variable {token.Content} used before value assignment"
                        );
                        return NoOpStatement;
                    }*/
                    return new VariableNode
                    {
                        // Type = type,
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

            return InputToken; // TODO: hmm
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
            var matchedToken = InputToken;
            if (InputTokenKeywordType == kwt)
            {
                NextToken();
                Console.WriteLine("matched keyword {0}", kwt);
                return matchedToken;
            }

            UnexpectedKeywordError(kwt);
            return matchedToken;
        }

        private Token MatchKeywordType(KeywordType[] kwtl)
        {
            var matchedToken = InputToken;
            foreach (var kwt in kwtl)
                if (InputTokenKeywordType == kwt)
                {
                    NextToken();
                    Console.WriteLine("matched keyword {0}", kwt);
                    return matchedToken;
                }

            UnexpectedKeywordError(kwtl);
            return matchedToken;
        }

        private bool MatchContent(string s)
        {
            if (InputTokenContent.Equals(s)) return true;
            ParseError(ErrorType.SyntaxError, InputToken, $"expected {s}, got {InputTokenContent}");
            return false;
        }

        public bool MatchContent(string[] sl)
        {
            if (sl.Any(InputTokenContent.Equals))
            {
                return true;
            }

            ParseError(ErrorType.SyntaxError, InputToken, $"expected one of {string.Join(", ", sl)}, got {InputTokenContent}");
            return false;
        }
    }
}