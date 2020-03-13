using System;
using System.Collections.Generic;
using System.Diagnostics;
using Compiler.Common;
using Compiler.Common.AST;
using Node = Compiler.Common.AST.Node;

namespace Compiler.Interpret
{
    public class Interpreter
    {
        private Node _tree;

        public Interpreter(Node tree, Text source)
        {
            _tree = tree;
            Visitor v = new ProgramVisitor(source);
            _tree.Accept(v);
        }
    }
}