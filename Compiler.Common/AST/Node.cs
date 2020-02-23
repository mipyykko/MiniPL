using System;
using System.Collections.Generic;
using Compiler.Interpret;

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
    }


    // public class EOFNode : Node
    // {
    //     public override PrimitiveType Type { get; }
    //
    //     public override void Accept(Visitor visitor) => visitor.Visit(this);
    // }
    //
    // public class OperatorNode : Node
    // {
    //     public OperatorType Operator;
    //
    //     public OperatorNode()
    //     {
    //     }
    //
    //     public override PrimitiveType Type { get; }
    //     public override void Accept(Visitor visitor) { visitor.Visit(this); }
    // }

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
        // {
        //     get
        //     {
        //         switch (Op.Type)
        //         {
        //             case TokenType.Multiplication:
        //             case TokenType.Division:
        //             case TokenType.Subtraction:
        //             case TokenType.Range:
        //             case TokenType.Addition when Left.Type == Right.Type && Left.Type == PrimitiveType.Int: 
        //                 return PrimitiveType.Int;
        //             case TokenType.Addition when Left.Type == Right.Type && Left.Type == PrimitiveType.String:
        //                 return PrimitiveType.String;
        //             case TokenType.Addition:
        //                 goto default;
        //             case TokenType.And:
        //             case TokenType.LessThan:
        //             case TokenType.Equals: 
        //                 return PrimitiveType.Bool;
        //             default:
        //                 throw new Exception("type error");
        //         }
        //     }
        // }

        public override object Accept(Visitor visitor) => visitor.Visit(this);
    }

    public abstract class ExpressionNode : Node
    {
        public Node Expression;
    }

    public class UnaryNode : ExpressionNode
    {
        public UnaryNode() {}

        public override PrimitiveType Type
        {
            get => PrimitiveType.Bool;
            set => Type = value;
        }

        public override object Accept(Visitor visitor) => visitor.Visit(this);
        
    }

    
    public class StatementNode : Node
    {
        public FunctionType Function;
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
    
    // public class VarDeclarationNode : NameNode
    // {
    //     public PrimitiveType DeclaredType;
    //     
    //     public VarDeclarationNode()
    //     {}
    //
    //     public override PrimitiveType Type => DeclaredType;
    //     public override void Accept(Visitor visitor) { visitor.Visit(this); }
    // }
    public class AssignmentNode : ExpressionNode
    {
        public bool Declaration = false;
        
        public AssignmentNode()
        {
        }
        
        public override PrimitiveType Type { get; set; }
        public override object Accept(Visitor visitor) => visitor.Visit(this);
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