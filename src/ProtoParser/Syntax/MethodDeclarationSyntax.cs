namespace ProtoParser.Syntax;

/// Represents a method declaration.
public class MethodDeclarationSyntax : SyntaxNode
{
    internal MethodDeclarationSyntax(
        IList< SyntaxToken > childTokens )
        : base( childTokens )
    {
    }
}