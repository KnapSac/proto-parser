namespace ProtoParser.Syntax;

/// Represents a service declaration.
public class ServiceDeclarationSyntax : SyntaxNode
{
    internal ServiceDeclarationSyntax(
        IList< SyntaxToken > childTokens )
        : base( childTokens )
    {
    }
}