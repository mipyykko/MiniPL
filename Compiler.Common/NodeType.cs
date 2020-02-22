namespace Compiler.Common
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
}