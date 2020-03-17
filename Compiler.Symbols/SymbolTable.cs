using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.InteropServices.ComTypes;
using Compiler.Common;
using static Compiler.Common.Util;

namespace Compiler.Symbols
{
    public class SymbolTable
    {
        private Dictionary<string, PrimitiveType> _symbols = new Dictionary<string, PrimitiveType>();
        private Dictionary<string, bool> _controlVariables = new Dictionary<string, bool>();

        public ErrorType DeclareSymbol(string id, PrimitiveType type)
        {
            if (_symbols.ContainsKey(id))
            {
                return ErrorType.RedeclaredVariable;
            }
            
            _symbols.Add(id, type);

            return ErrorType.Unknown;
        }

        public PrimitiveType LookupSymbol(string id)
        {
            if (!_symbols.ContainsKey(id))
            {
                throw new Exception("undeclared variable");
                // return ErrorType.UndeclaredVariable; // TODO: throw exceptions or smth
            }

            return _symbols[id];
        }
        
        public bool SymbolExists(string id) => _symbols.ContainsKey(id);

        public ErrorType SetControlVariable(string id)
        {
            if (!_symbols.ContainsKey(id))
            {
                return ErrorType.UndeclaredVariable; // TODO
            }
            _controlVariables[id] = true;

            return ErrorType.Unknown;
        }

        public ErrorType UnsetControlVariable(string id)
        {
            if (!_controlVariables.ContainsKey(id))
            {
                return ErrorType.AssignmentToControlVariable; // TODO
            }
            _controlVariables[id] = false;

            return ErrorType.Unknown;
        }
        
        public bool IsControlVariable(string id)
        {
            return _controlVariables.ContainsKey(id) && _controlVariables[id];
        }
    }
}