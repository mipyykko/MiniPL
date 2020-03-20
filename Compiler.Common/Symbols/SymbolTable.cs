using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.InteropServices.ComTypes;
using Compiler.Common;
using static Compiler.Common.Util;

namespace Compiler.Common
{
    public class SymbolTable : ISymbolTable
    {
        private Dictionary<string, PrimitiveType> _symbols = new Dictionary<string, PrimitiveType>();
        private Dictionary<string, bool> _controlVariables = new Dictionary<string, bool>();

        public virtual ErrorType DeclareSymbol(string id, PrimitiveType type)
        {
            if (_symbols.ContainsKey(id))
            {
                return ErrorType.RedeclaredVariable;
            }
            
            _symbols.Add(id, type);

            return ErrorType.Unknown;
        }

        public virtual PrimitiveType LookupSymbol(string id)
        {
            if (!_symbols.ContainsKey(id))
            {
                return PrimitiveType.Void;
                // return ErrorType.UndeclaredVariable; // TODO: throw exceptions or smth
            }

            return _symbols[id];
        }
        
        public bool SymbolExists(string id) => _symbols.ContainsKey(id);

        public virtual ErrorType SetControlVariable(string id)
        {
            if (!_symbols.ContainsKey(id))
            {
                return ErrorType.UndeclaredVariable; // TODO
            }
            _controlVariables[id] = true;

            return ErrorType.Unknown;
        }

        public virtual ErrorType UnsetControlVariable(string id)
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