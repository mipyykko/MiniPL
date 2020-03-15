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
        private Dictionary<string, (PrimitiveType Type, object Value)> Symbols = new Dictionary<string, (PrimitiveType, object)>();
        private Dictionary<string, bool> ControlVariables = new Dictionary<string, bool>();

        public ErrorType UpdateSymbol(string id, PrimitiveType type, object value = null, bool control = false)
        {
            if (!control && ControlVariables.TryGetValueOrDefault(id))
            {
                return ErrorType.AssignmentToControlVariable;
            }

            Symbols[id] = (type, ParseResult(type, value));
            return ErrorType.Unknown;
        }
        
        public ErrorType UpdateSymbol(string id, object value, bool control = false)
        {
            return UpdateSymbol(id, Symbols.TryGetValueOrDefault(id).Type, value, control);
        }

        public ErrorType DeclareSymbol(string id, PrimitiveType type)
        {
            return Symbols.ContainsKey(id) ? ErrorType.RedeclaredVariable : UpdateSymbol(id, type);
        }

        public ErrorType UpdateControlVariable(string id, object value)
        {
            if (!ControlVariables.ContainsKey(id) || !ControlVariables[id])
            {
                return ErrorType.AssignmentToControlVariable; // TODO
            }
            return UpdateSymbol(id, value, true);
        }

        public ErrorType SetControlVariable(string id)
        {
            if (ControlVariables.ContainsKey(id) && ControlVariables[id])
            {
                return ErrorType.AssignmentToControlVariable; // TODO
            }
            ControlVariables[id] = true;

            return ErrorType.Unknown;
        }

        public ErrorType UnsetControlVariable(string id)
        {
            if (!ControlVariables.ContainsKey(id) || !ControlVariables[id])
            {
                return ErrorType.AssignmentToControlVariable; // TODO
            }
            ControlVariables[id] = false;

            return ErrorType.Unknown;
        }

        public bool IsControlVariable(string id) => ControlVariables.TryGetValueOrDefault(id);

        public object LookupSymbol(string id)
        {
            if (!Symbols.ContainsKey(id))
            {
                return null;
            }

            return Symbols[id];
        }

        public object LookupValue(string id) => (((PrimitiveType, object)?) LookupSymbol(id))?.Item2;

        public object LookupType(string id) => (((PrimitiveType, object)?) LookupSymbol(id))?.Item1;

        public bool SymbolExists(string id) => Symbols.ContainsKey(id);

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
                Console.WriteLine($"type error: expected {type}, got {GuessType((string) value)}");
                return null;
                
            }
        }
    }
}