using System;
using System.Collections.Generic;
using MiniPL.Common;
using MiniPL.Common.Symbols;

namespace MiniPL.Interpret
{
    public class ProgramMemory : IProgramMemory
    {
        private readonly Dictionary<string, object> _memory = new Dictionary<string, object>();
        private ISymbolTable SymbolTable => Context.SymbolTable;
        
        public ErrorType UpdateVariable(string id, dynamic value = null, bool control = false)
        {
            if (!control && SymbolTable.IsControlVariable(id))
            {
                return ErrorType.AssignmentToControlVariable;
            }

            try
            {
                _memory[id] = ParseResult(SymbolTable.LookupSymbol(id), value); // TODO: error?
            }
            catch (Exception)
            {
                return ErrorType.TypeError;
            }

            return ErrorType.Unknown;
        }

        public ErrorType UpdateControlVariable(string id, object value)
        {
            // if (!_symbolTable.IsControlVariable(id) || !_symbolTable.SymbolExists(id))
            // {
            //     return ErrorType.AssignmentToControlVariable; // TODO
            // }
            return UpdateVariable(id, value, true);
        }


        public dynamic LookupVariable(string id)
        {
            return _memory[id];
        }
        
        public dynamic ParseResult(PrimitiveType type, dynamic value)
        {

            // try
            // {
                return type switch
                {
                    PrimitiveType.Int when value is string s => int.Parse(s),
                    PrimitiveType.String => (string) value,
                    PrimitiveType.Bool when value is string s => bool.Parse(s), // TODO: hmm
                    _ => value
                };
            /*}
            catch (Exception)
            {
                // TODO: error?
                ErrorService.Add(Error.Of(
                        ErrorType.TypeError,
                        
                        )
                    )
                throw new Exception($"type error: expected {type}, got {GuessType((string) value)}");
                return null;
                
            }*/
        }
    }
}