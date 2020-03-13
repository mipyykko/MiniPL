using System;
using System.Collections.Generic;
using Compiler.Interpret;
using static Compiler.Common.Util;

namespace Compiler.Common.AST
{
    public abstract class Node
    {
        public virtual string Name => "Node";
        
        public Node Parent { get; set; }
        public object Value { get; set; }
        public Token Token { get; set; }
        public abstract PrimitiveType Type { get; set; }
        public override string ToString()
        {
            return $"{Type} {Value}";
        }

        public abstract object Accept(Visitor visitor);

        public virtual void AST(int depth = 0, string caller = "")
        {
            Console.Write($"{Name}");
        }
    }
    
    public abstract class OpNode : Node
    {
        public Token Op;
    }
    
    public class BinaryNode : Node
    {
        public override string Name => "BinaryOp";

        public Node Left;
        public Node Right;
        
        public override PrimitiveType Type { get; set; }

        public override object Accept(Visitor visitor) => visitor.Visit(this);
        public override void AST(int depth = 0, string caller = "")
        {
            // Console.Write($"{Spaces(depth * 2)}[");
            Console.WriteLine($"{Name} {Token.Content}");
            Left.AST(depth + 1);
            Right.AST(depth + 1);
            Console.WriteLine($"{Spaces(depth * 2)}]");
        }
    }

    public class ExpressionNode : Node
    {
        public override string Name => "Expression";

        public Node Expression;

        public override PrimitiveType Type { get; set; }
        public override object Accept(Visitor visitor) => visitor.Visit(this);

        public override void AST(int depth = 0, string caller = "")
        {
            // base.AST(depth, "expressionnode");
            Console.Write($"{Spaces(depth * 2)}[");
            Expression.AST(depth);
        }
    }

    public class UnaryNode : Node
    {
        public override string Name => "UnaryOp";

        public new Node Value; // TODO

        public override PrimitiveType Type
        {
            get => Value.Type;
            set => Type = value;
        }

        public override object Accept(Visitor visitor) => visitor.Visit(this);
        public override void AST(int depth = 0, string caller = "")
        {
            Console.Write($"{Name} {Token.Content}");
            Value.AST(depth + 1);
        }
    }

    
    public class StatementNode : Node
    {
        public override string Name => "Statement";

        public List<Node> Arguments { get; set; }
        
        public override PrimitiveType Type
        {
            get => PrimitiveType.Void;
            set => Type = value;
        }

        public override object Accept(Visitor visitor) => visitor.Visit(this);
        public override void AST(int depth = 0, string caller = "")
        {
            Console.Write($"{Spaces(depth * 2)}[");
            base.AST(depth);
            Console.WriteLine($"\n{Spaces((depth + 1) * 2)}[{Token.Content}]");
            foreach (var n in Arguments)
            {
                n.AST(depth + 1);
            }
            Console.WriteLine($"{Spaces(depth * 2)}]");
        }
    }

    public class NoOpNode : Node
    {
        public override string Name => "NoOp";

        public override PrimitiveType Type { get; set; }
        public override object Accept(Visitor visitor) => visitor.Visit(this);
        public override void AST(int depth = 0, string caller = "")
        {
            Console.Write($"{Spaces(depth * 2)}[");
            base.AST(depth);
            Console.WriteLine($"]");
        }
    }

    
    public class VariableNode : Node
    {
        public override string Name => "Variable";

        public override PrimitiveType Type {
            get
            {
                return Token.Type switch
                {
                    TokenType.IntValue => PrimitiveType.Int,
                    TokenType.BoolValue => PrimitiveType.Bool,
                    TokenType.StringValue => PrimitiveType.String,
                    _ => PrimitiveType.Void
                };
            }
            set => Type = value;
        } 

        public override object Accept(Visitor visitor) => visitor.Visit(this);

        public override void AST(int depth = 0, string caller = "")
        {
            Console.WriteLine($"{Spaces(depth * 2)}[{Name} {Type}");
            Console.WriteLine($"{Spaces((depth + 1) * 2)}[{Token.Content}]");
            Console.WriteLine($"{Spaces(depth * 2)}]");
        }
    }
    
    public class AssignmentNode : ExpressionNode
    {
        public override string Name => "Assignment";

        public Node Id;
        
        public override PrimitiveType Type { get; set; }
        public override object Accept(Visitor visitor) => visitor.Visit(this);
        public override void AST(int depth = 0, string caller = "")
        {
            Console.Write($"{Spaces(depth * 2)}[");
            Console.WriteLine($"{Name} {Type}");
            Id.AST(depth + 1);
            Expression.AST(depth + 1);
            Console.WriteLine($"{Spaces(depth * 2)}]");
        }
    }

    // public class ErrorNode : Node
    // {
    //     public override string Name => "Error";
    //     public ErrorType Error;
    //     public override PrimitiveType Type { get; set; }
    //     public override object Accept(Visitor visitor) => visitor.Visit(this);
    //
    //     public override void AST(int depth = 0, string caller = "")
    //     {
    //         Console.WriteLine($"{Spaces(depth * 2)}[{Error} {Token}]");
    //     }
    // }

    public class StatementListNode : Node
    {
        public override string Name => "StatementList";

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
        public override void AST(int depth = 0, string caller = "")
        {
            Console.Write($"{Spaces(depth * 2)}[");
            base.AST(depth);
            Console.WriteLine();
            Left.AST(depth + 1);
            Right.AST(depth + 1);
            Console.WriteLine($"{Spaces(depth * 2)}]");
        }
    }

    public class LiteralNode : Node
    {
        // public PrimitiveType ValueType;
        public override string Name => "Literal";

        public override PrimitiveType Type { get; set; }

        public override object Accept(Visitor visitor) => visitor.Visit(this);
        public override void AST(int depth = 0, string caller = "")
        {
            Console.Write($"{Spaces(depth * 2)}[");
            base.AST(depth);
            Console.WriteLine($"\n{Spaces((depth + 1) * 2)}[{Token.Content}]");
            
            Console.WriteLine($"{Spaces(depth * 2)}]");
        }
    }

    public class ForNode : Node
    {
        public override string Name => "For";

        public Node RangeStart;
        public Node RangeEnd;
        public Node Statements;           

        public override PrimitiveType Type { get => PrimitiveType.Void; set => throw new Exception(""); }

        public override object Accept(Visitor visitor) => visitor.Visit(this);
        public override void AST(int depth = 0, string caller = "")
        {
            Console.Write($"{Spaces(depth * 2)}[");
            base.AST(depth);
            Console.WriteLine();
            RangeStart.AST(depth + 1);
            RangeEnd.AST(depth + 1);
            Statements.AST(depth + 1);
            Console.WriteLine($"{Spaces(depth * 2)}]");
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