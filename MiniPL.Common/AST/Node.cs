using System;
using System.Collections.Generic;
using static MiniPL.Common.Util;

namespace MiniPL.Common.AST
{
    public abstract class Node
    {
        public virtual string Name => "Node";

        public Node Parent { get; set; }
        public dynamic Value { get; set; }
        public abstract Token Token { get; set; }
        public abstract PrimitiveType Type { get; set; }

        public override string ToString()
        {
            return $"{Type} {Value}";
        }

        public abstract dynamic Accept(Visitor visitor);

        public virtual void AST(int depth = 0)
        {
            Console.Write($"{Name}");
        }

        public virtual string Representation() => $"{Token.Content}";
    }

    public abstract class OpNode : Node
    {
        public abstract OperatorType Operator { get; set; }
    }
    
    public class BinaryNode : OpNode
    {
        public override string Name => "BinaryOp";

        public Node Left;
        public Node Right;

        public override OperatorType Operator { get; set; }

        public override Token Token { get; set; }
        public override PrimitiveType Type { get; set; }

        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);

        public override void AST(int depth = 0)
        {
            // Console.Write($"{Spaces(depth * 2)}[");
            Console.WriteLine($"{Name} {Token.Content}");
            Left.AST(depth + 1);
            Right.AST(depth + 1);
            Console.WriteLine($"{Spaces(depth * 2)}]");
        }

        public override string Representation() => $"{Left.Representation()} {Token.Content} {Right.Representation()}";
    }

    public class ExpressionNode : Node
    {
        public override string Name => "Expression";

        public Node Expression;

        public override Token Token { get; set; }
        public override PrimitiveType Type { get; set; }
        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);

        public override void AST(int depth = 0)
        {
            // base.AST(depth, "expressionnode");
            Console.Write($"{Spaces(depth * 2)}[");
            Expression.AST(depth);
        }

        public override string Representation() => Expression.Representation();
    }

    public class UnaryNode : OpNode
    {
        public override string Name => "UnaryOp";

        public new Node Value;

        public override OperatorType Operator { get; set; }
        
        public override Token Token { get; set; }
        public override PrimitiveType Type { get; set; }

        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);

        public override void AST(int depth = 0)
        {
            Console.WriteLine($"{Name} {Token.Content}");
            Value.AST(depth + 1);
            Console.WriteLine($"{Spaces(depth * 2)}]");
        }

        public override string Representation()
        {
            return $"{Token.Content}{Value.Representation()}";
        }

    }


    public class StatementNode : Node
    {
        public override string Name => "Statement";

        public List<Node> Arguments { get; set; }

        public override Token Token { get; set; }
        public override PrimitiveType Type { get; set; } = PrimitiveType.Void;

        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);

        public override void AST(int depth = 0)
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
        public override Token Token { get; set; }
        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);

        public override void AST(int depth = 0)
        {
            Console.Write($"{Spaces(depth * 2)}[");
            base.AST(depth);
            Console.WriteLine($"]");
        }
    }


    public class VariableNode : Node
    {
        public override string Name => "Variable";

        public override PrimitiveType Type { get; set; }

        public override Token Token { get; set; }

        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);

        public override void AST(int depth = 0)
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
        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);

        public override void AST(int depth = 0)
        {
            Console.Write($"{Spaces(depth * 2)}[");
            Console.WriteLine($"{Name} {Type}");
            Id.AST(depth + 1);
            Expression.AST(depth + 1);
            Console.WriteLine($"{Spaces(depth * 2)}]");
        }
    }
    
    public class StatementListNode : Node
    {
        public override string Name => "StatementList";

        public Node Left;
        public Node Right;

        public override PrimitiveType Type
        {
            get => PrimitiveType.Void;
            set => throw new InvalidOperationException();
        }

        public override Token Token { get; set; }

        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);

        public override void AST(int depth = 0)
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
        public override string Name => "Literal";

        public override PrimitiveType Type { get; set; }

        // {
        //     get => TokenToPrimitiveType(Token.Type);
        //     set => throw new Exception();     
        // } 
        public override Token Token { get; set; }

        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);

        public override void AST(int depth = 0)
        {
            Console.WriteLine($"{Spaces(depth * 2)}[{Name} {Type}");
            Console.WriteLine($"{Spaces((depth + 1) * 2)}[{Token.Content}]");

            Console.WriteLine($"{Spaces(depth * 2)}]");
        }
    }

    public class ForNode : Node
    {
        public override string Name => "For";

        public Node Id;
        public Node RangeStart;
        public Node RangeEnd;
        public Node Statements;

        public override PrimitiveType Type
        {
            get => PrimitiveType.Void;
            set => throw new InvalidOperationException();
        }

        public override Token Token { get; set; }
        public override dynamic Accept(Visitor visitor) => visitor.Visit(this);

        public override void AST(int depth = 0)
        {
            Console.Write($"{Spaces(depth * 2)}[");
            base.AST(depth);
            Console.WriteLine();
            Id.AST(depth + 1);
            RangeStart.AST(depth + 1);
            RangeEnd.AST(depth + 1);
            Statements.AST(depth + 1);
            Console.WriteLine($"{Spaces(depth * 2)}]");
        }
    }
}