using System;
using System.Collections.Generic;
using System.Xml;
using Compiler.Interpret;
using static Compiler.Common.Util;

namespace Compiler.Common.AST
{
    public abstract class Node
    {
        public Node Parent { get; set; }
        public object Value { get; set; }
        public Token Token { get; set; }
        public abstract PrimitiveType Type { get; set; }
        public override string ToString()
        {
            return $"{Type} {Value}";
        }

        public abstract object Accept(Visitor visitor);

        public virtual void AST(int depth = 0)
        {
            Console.WriteLine($"{Spaces(depth * 2)}{GetType()} {Token}");
        }
    }
    
    public abstract class OpNode : Node
    {
        public Token Op;
    }
    
    public class BinaryNode : OpNode
    {
        public Node Left;
        public Node Right;
        
        public BinaryNode() {
        }
        
        public override PrimitiveType Type { get; set; }

        public override object Accept(Visitor visitor) => visitor.Visit(this);
        public override void AST(int depth = 0)
        {
            base.AST(depth);
            Left.AST(depth + 1);
            Right.AST(depth + 1);
        }
    }

    public class ExpressionNode : Node
    {
        public Node Expression;

        public override PrimitiveType Type { get; set; }
        public override object Accept(Visitor visitor) => visitor.Visit(this);

        public override void AST(int depth = 0)
        {
            base.AST(depth);
            Expression.AST(depth + 1);
        }
    }

    public class UnaryNode : OpNode
    {
        public new Node Value; // TODO
        public UnaryNode() {}

        public override PrimitiveType Type
        {
            get => Value.Type;
            set => Type = value;
        }

        public override object Accept(Visitor visitor) => visitor.Visit(this);
        public override void AST(int depth = 0)
        {
            base.AST(depth);
            Value.AST(depth + 1);
        }
    }

    
    public class StatementNode : Node
    {
        public List<Node> Arguments { get; set; }

        public StatementNode()
        {
        }

        public override PrimitiveType Type
        {
            get => PrimitiveType.Void;
            set => Type = value;
        }

        public override object Accept(Visitor visitor) => visitor.Visit(this);
        public override void AST(int depth = 0)
        {
            base.AST(depth);
            foreach (var n in Arguments)
            {
                n.AST(depth + 1);
            }
        }
    }

    public class NoOpNode : Node
    {
        public override PrimitiveType Type { get; set; }
        public override object Accept(Visitor visitor) => visitor.Visit(this);
    }

    
    public class VariableNode : Node
    {
        public VariableNode()
        {}

        public override PrimitiveType Type {
            get
            {
                switch (Token.Type)
                {
                    case TokenType.IntValue:
                        return PrimitiveType.Int;
                    case TokenType.BoolValue:
                        return PrimitiveType.Bool;
                    case TokenType.StringValue:
                        return PrimitiveType.String;
                    default:
                        return PrimitiveType.Void;
                }
            }
            set => Type = value;
        } 

        public override object Accept(Visitor visitor) => visitor.Visit(this);
    }
    
    public class AssignmentNode : ExpressionNode
    {
        public Node Id;
        
        public AssignmentNode()
        {
        }
        
        public override PrimitiveType Type { get; set; }
        public override object Accept(Visitor visitor) => visitor.Visit(this);
        public override void AST(int depth = 0)
        {
            base.AST(depth);
            Id.AST(depth + 1);
            Expression.AST(depth + 1);
        }
    }

    // public class ProgramNode : Node
    // {
    //     public Node Left;
    //
    //     public ProgramNode()
    //     {
    //     }
    //
    //     public override PrimitiveType Type => PrimitiveType.Void;
    //     public override void Accept(Visitor visitor) { visitor.Visit(this); }
    // }

    public class StatementListNode : Node
    {
        public Node Left;
        public Node Right;

        public StatementListNode()
        {
        }
        
        public override PrimitiveType Type
        {
            get => PrimitiveType.Void; 
            set => Type = value; 
        }

        public override object Accept(Visitor visitor) => visitor.Visit(this);
        public override void AST(int depth = 0)
        {
            base.AST(depth);
            Left.AST(depth + 1);
            Right.AST(depth + 1);
        }
    }

    public class LiteralNode : Node
    {
        // public PrimitiveType ValueType;

        public LiteralNode()
        {
        }

        public override PrimitiveType Type { get; set; }

        public override object Accept(Visitor visitor) => visitor.Visit(this);
    }

    public class ForNode : Node
    {
        public Node RangeStart;
        public Node RangeEnd;
        public Node Statements;           

        public override PrimitiveType Type { get => PrimitiveType.Void; set => throw new Exception(""); }

        public override object Accept(Visitor visitor) => visitor.Visit(this);
        public override void AST(int depth = 0)
        {
            base.AST(depth);
            RangeStart.AST(depth + 1);
            RangeEnd.AST(depth + 1);
            Statements.AST(depth + 1);
        }
    }
    // public class NameNode : Node
    // {
    //     public string Name;
    //
    //     public NameNode()
    //     {            
    //     }
    //
    //     public override PrimitiveType Type { get; }
    //     public override void Accept(Visitor visitor) { visitor.Visit(this); }
    // }
}