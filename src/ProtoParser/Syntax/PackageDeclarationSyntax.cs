namespace ProtoParser.Syntax;

/// Represents a package declaration.
public class PackageDeclarationSyntax : SyntaxNode
{
    internal PackageDeclarationSyntax(
        IList< SyntaxToken > childTokens )
        : base( childTokens )
    {
    }
}