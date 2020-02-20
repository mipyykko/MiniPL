using System;
using System.Collections.Generic;
using System.Text;

namespace Compiler.Common
{
    public class Node
    {
        public enum NodeType
        {
            Unknown,
            Program,
            StatementList,
            Statement,
            VariableDeclaration,
            VariableAssignment,
            Identifier,
            For,
            Read,
            Print,
            Assert,
            BinaryExpression,
            UnaryExpression,
            ValueExpression,
            IntValue,
            StringValue,
            BoolValue,
            Addition,
            Subtraction,
            Multiplication,
            Division,
            And,
            Not,
            Equals,
            LessThan,
            IntType,
            BoolType,
            StringType,
            AnyType,
            EOF
        }

        public static Dictionary<KeywordType, NodeType> KeywordToNodeType = new Dictionary<KeywordType, NodeType>()
        {
            [KeywordType.Int] = NodeType.IntType,
            [KeywordType.Bool] = NodeType.BoolType,
            [KeywordType.String] = NodeType.StringType
        };

        public static Dictionary<NodeType, NodeType> TypeToValue = new Dictionary<NodeType, NodeType>()
        {
            [NodeType.IntType] = NodeType.IntValue,
            [NodeType.BoolType] = NodeType.BoolValue,
            [NodeType.StringType] = NodeType.StringValue
        };

        public static Dictionary<TokenType, NodeType> TokenToNodeType = new Dictionary<TokenType, NodeType>()
        {
            [TokenType.IntValue] = NodeType.IntValue,
            [TokenType.StringValue] = NodeType.StringValue,
            [TokenType.BoolValue] = NodeType.BoolValue,
            [TokenType.Identifier] = NodeType.Identifier
        };

        public static Dictionary<string, NodeType> OperatorToNodeType = new Dictionary<string, NodeType>()
        {
            ["*"] = NodeType.Multiplication,
            ["/"] = NodeType.Division,
            ["+"] = NodeType.Addition,
            ["-"] = NodeType.Subtraction,
            ["&"] = NodeType.And,
            ["="] = NodeType.Equals,
            ["<"] = NodeType.LessThan
        };

        public List<Node> Children { get; private set; }
        public Node Parent { get; set; }
        public NodeType Type { get; private set; }
        public string Value { get; set; }

        private Node(NodeType type, List<Node> children)
        {
            Type = type;
            Children = children;
        }

        private Node(NodeType type) : this(type, new List<Node>()) { }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            return $"{Parent?.Type.ToString()} {Type}: {Value}";
        }

        public void AddChild(Node node)
        {
            node.Parent = this;
            Children.Add(node);
        }

        public void RemoveChild(Node node)
        {
            node.Parent = null;
            Children.Remove(node);
        }

        public static Node Of(NodeType type)
        {
            return new Node(type);
        }
        public static Node Of(NodeType type, params Node[] children)
        {
            Node n = Of(type);
            n.Children = new List<Node>(children);

            foreach (Node child in n.Children)
            {
                child.Parent = n;
            }
            return n;
        }

        public static void PrintTree(Node node, int depth = 0)
        {
            if (node == null)
            {
                return;
            }

            Console.WriteLine($"{new string(' ', depth * 2)}{node}");

            foreach (Node child in node.Children)
            {
                PrintTree(child, depth + 1);
            }
        }

    }
}
