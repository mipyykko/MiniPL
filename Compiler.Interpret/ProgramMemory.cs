using System;
using System.Collections.Generic;
using Compiler.Common;
using Compiler.Symbols;
using static Compiler.Common.Util;

namespace Compiler.Interpret
{
    internal class ProgramMemory
    {
        private readonly Dictionary<string, object> _memory = new Dictionary<string, object>();
        private readonly SymbolTable _symbolTable;

        public ProgramMemory(SymbolTable symbolTable)
        {
            _symbolTable = symbolTable;
        }
        
        public ErrorType UpdateVariable(string id, object value = null, bool control = false)
        {
            if (!control && _symbolTable.IsControlVariable(id))
            {
                return ErrorType.AssignmentToControlVariable;
            }

            _memory[id] = ParseResult((PrimitiveType) _symbolTable.LookupSymbol(id), value); // TODO: error?
            return ErrorType.Unknown;
        }

        public ErrorType UpdateControlVariable(string id, object value)
        {
            if (!_symbolTable.IsControlVariable(id) || !_symbolTable.SymbolExists(id))
            {
                return ErrorType.AssignmentToControlVariable; // TODO
            }
            return UpdateVariable(id, value, true);
        }


        public object LookupVariable(string id)
        {
            if (!_memory.ContainsKey(id))
            {
                return ErrorType.UndeclaredVariable; // TODO
            }

            return _memory[id];
        }
        
        public object ParseResult(PrimitiveType type, object value)
        {

            try
            {
                return type switch
                {
                    PrimitiveType.Int when value is string s => int.Parse(s),
                    PrimitiveType.String => (string) value,
                    PrimitiveType.Bool when value is string s => s.ToLower().Equals("true"),
                    _ => value
                };
            }
            catch (Exception)
            {
                // TODO: error?
                throw new Exception($"type error: expected {type}, got {GuessType((string) value)}");
                return null;
                
            }
        }
    }
}