using System;
using System.Collections.Generic;
using System.Text;
using Compiler.Common;
using Compiler.Common.Errors;

namespace Compiler.Scan
{
    public class Scanner
    {
        private Text Source => Context.Source;
        private IErrorService ErrorService => Context.ErrorService;
        private int _startPos;
        private int _startLinePos;

        private char Current => Source.Current;
        private char Peek => Source.Peek;
        private bool IsExhausted => Source.IsExhausted;

        private (int Start, int End) TokenRange(string token)
        {
            return (_startPos, _startPos + Math.Max(0, token.Length - 1));
        }

        private (int Line, int Start, int End) TokenLineRange(string token)
        {
            return (Source.Line, _startLinePos, _startLinePos + Math.Max(0, token.Length - 1));
        }

        private SourceInfo GetSourceInfo(string token)
        {
            return SourceInfo.Of(
                TokenRange(token),
                TokenLineRange(token)
            );
        }

        public Token GetNextToken()
        {
            Source.SkipSpacesAndComments();

            if (IsExhausted) return Token.Of(TokenType.EOF, GetSourceInfo(""));

            _startPos = Source.Pos;
            _startLinePos = Source.LinePos;
            var (tokenType, token) = GetTrivialToken();

            switch (tokenType)
            {
                case TokenType.Number:
                {
                    var numberContents = GetNumberContents(); // TODO: error check
                    return Token.Of(TokenType.IntValue, numberContents, GetSourceInfo(numberContents));
                }
                case TokenType.Quote:
                    var stringContents = GetStringContents(); // TODO: error check
                    return Token.Of(TokenType.StringValue, stringContents, GetSourceInfo(stringContents));
                case TokenType.Unknown when char.IsLetter(Current):
                    var atom = GetAtom();
                    var kw = Token.GetKeywordType(atom.ToLower());

                    if (kw == KeywordType.False || kw == KeywordType.True)
                        return Token.Of(TokenType.BoolValue, atom, GetSourceInfo(atom));
                    if (kw != KeywordType.Unknown) return Token.Of(TokenType.Keyword, kw, atom, GetSourceInfo(atom));
                    return Token.Of(TokenType.Identifier, atom, GetSourceInfo(atom));
                case TokenType.Unknown:
                    return Token.Of(TokenType.Unknown, $"{Current}", GetSourceInfo($"{Current}")); // TODO: error check?
                default:
                    return Token.Of(tokenType, token, GetSourceInfo(token));
            }
        }

        private (TokenType, string) GetTrivialToken()
        {
            if (Text.IsDigit(Current)) return (TokenType.Number, $"{Current}");
            if (char.IsLetter(Current)) // we don't have any trivial tokens starting with letters now
                return (TokenType.Unknown, $"{Current}");

            foreach (var token in Token.TrivialTokenTypes.Keys)
                if (Source.Pos + token.Length <= Source.End &&
                    Source.Range(Source.Pos, token.Length).ToLower().Equals(token.ToLower()))
                {
                    Source.Advance(token.Length);
                    return (Token.TrivialTokenTypes[token], token);
                }

            return (TokenType.Unknown, $"{Current}");
        }

        private string GetAtom()
        {
            var kw = new StringBuilder("");

            while (!Source.IsExhausted && " \t\n".IndexOf(Current) < 0 && (char.IsLetter(Current) ||
                                                                           char.IsDigit(Current) || Current == '_'))
            {
                kw.Append(Current);
                Source.Advance();
            }

            return kw.ToString();
        }

        private string GetNumberContents()
        {
            var n = new StringBuilder("");

            while (!Source.IsExhausted && Text.IsDigit(Current))
            {
                n.Append(Current);
                Source.Advance();
            }

            return n.ToString();
        }

        private Dictionary<char, string> Literals = new Dictionary<char, string>()
        {
            ['n'] = "\n",
            ['t'] = "\t",
            ['\\'] = "\\"
        };

        private string GetStringContents()
        {
            var str = new StringBuilder();

            string error = null;

            while (!Source.IsExhausted)
            {
                var peekedLiteral = Literals.TryGetValueOrDefault(Peek);

                switch (Current)
                {
                    case '"':
                        Source.Advance();
                        return str.ToString();
                    case '\\' when peekedLiteral == null && Text.IsDigit(Peek):
                        Source.Advance();
                        var number = GetNumberContents();
                        try
                        {
                            int numberValue = short.Parse(number);
                            str.Append(Convert.ToChar(numberValue));
                        }
                        catch
                        {
                            ErrorService.Add(
                                ErrorType.SyntaxError,
                                Token.Of(
                                    TokenType.Unknown,
                                    SourceInfo.Of(TokenRange($"{Current}"), TokenLineRange($"{Current}"))),
                                $"unknown special character \\{number}"
                            );
                        }
                        break;
                    case '\\' when peekedLiteral == null:
                        ErrorService.Add(
                            ErrorType.SyntaxError,
                            Token.Of(
                                TokenType.Unknown,
                                SourceInfo.Of(TokenRange($"{Current}"), TokenLineRange($"{Current}"))),
                            $"unknown special character \\{Peek}"
                        );
                        Source.Advance();
                        break;
                    case '\\':
                        str.Append(peekedLiteral);
                        Source.Advance(2);
                        break;
                    default:
                        str.Append(Current);
                        Source.Advance();
                        break;
                }
            }

            ErrorService.Add(
                ErrorType.UnterminatedStringTerminal,
                Token.Of(
                    TokenType.Unknown,
                    SourceInfo.Of(TokenRange($"{Current}"), TokenLineRange($"{Current}"))),
                $"unterminated string terminal",
                true);
            return "";
        }
    }
}