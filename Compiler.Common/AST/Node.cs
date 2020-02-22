using System;
using System.Collections.Generic;
using Compiler.Interpret;

namespace Compiler.Common.AST
{
    public abstract class Node
    {
        public Node Parent { get; set; }
        public Node Value { get; set; }

        public abstract PrimitiveType Type { get; }
        public override string ToString()
        {
            return $"{Type} {Value}";
        }

        public abstract void Accept(Visitor visitor);
    }

    public class EOFNode : Node
    {
        public override PrimitiveType Type { get; }

        public override void Accept(Visitor visitor) => visitor.Visit(this);
    }
    
    public class OperatorNode : Node
    {
        public OperatorType Operator;

        public OperatorNode()
        {
        }

        public override PrimitiveType Type { get; }
        public override void Accept(Visitor visitor) { visitor.Visit(this); }
    }
    public class BinaryNode : OperatorNode
    {
        public Node Left;
        public Node Right;

        public BinaryNode() {
        }
        
        public override PrimitiveType Type
        {
            get
            {
                switch (Operator)
                {
                    case OperatorType.Multiplication:
                    case OperatorType.Division:
                    case OperatorType.Subtraction:
                    case OperatorType.Addition when Left.Type == Right.Type && Left.Type == PrimitiveType.Int: 
                        return PrimitiveType.Int;
                    case OperatorType.Addition when Left.Type == Right.Type && Left.Type == PrimitiveType.String:
                        return PrimitiveType.String;
                    case OperatorType.Addition:
                        goto default;
                    case OperatorType.And:
                    case OperatorType.LessThan:
                    case OperatorType.Equals:
                        return PrimitiveType.Bool;
                    default:
                        throw new Exception("type error");
                }
            }
        }

        public override void Accept(Visitor visitor) { visitor.Visit(this); }
    }

    public class UnaryNode : OperatorNode
    {
        public Node Value;
        
        public UnaryNode() {}

        public override PrimitiveType Type => PrimitiveType.Bool;

        public override void Accept(Visitor visitor) { visitor.Visit(this); }
        
    }

    public class StatementNode : Node
    {
        public FunctionType Function;
        public List<Node> Arguments { get; set; }

        public StatementNode()
        {
        }

        public override PrimitiveType Type => PrimitiveType.Void;

        public override void Accept(Visitor visitor) { visitor.Visit(this); }
    }

    public class IdentifierNode : NameNode
    {
        public IdentifierNode()
        {}

        public override PrimitiveType Type => PrimitiveType.Unknown;
        public override void Accept(Visitor visitor) { visitor.Visit(this); }
    }
    
    public class VarDeclarationNode : NameNode
    {
        public PrimitiveType DeclaredType;
        
        public VarDeclarationNode()
        {}

        public override PrimitiveType Type => DeclaredType;
        public override void Accept(Visitor visitor) { visitor.Visit(this); }
    }
    public class VarAssignmentNode : NameNode
    {

        public VarAssignmentNode()
        {
        }
        
        public override PrimitiveType Type => Value.Type;
        public override void Accept(Visitor visitor) { visitor.Visit(this); }
    }

    public class ProgramNode : Node
    {
        public Node Left;

        public ProgramNode()
        {
        }

        public override PrimitiveType Type => PrimitiveType.Void;
        public override void Accept(Visitor visitor) { visitor.Visit(this); }
    }

    public class StatementListNode : Node
    {
        public Node Left;
        public Node Right;

        public StatementListNode()
        {
        }
        
        public override PrimitiveType Type => PrimitiveType.Void;
        public override void Accept(Visitor visitor) { visitor.Visit(this); }
    }

    public class LiteralNode : Node
    {
        public object Value;
        public PrimitiveType ValueType;

        public LiteralNode()
        {
        }

        public override PrimitiveType Type => ValueType;
        public override void Accept(Visitor visitor) { visitor.Visit(this); }
    }

    public class NameNode : Node
    {
        public string Name;

        public NameNode()
        {            
        }

        public override PrimitiveType Type { get; }
        public override void Accept(Visitor visitor) { visitor.Visit(this); }
    }
}