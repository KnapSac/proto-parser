namespace ProtoParser.Syntax;

/// Represents a non-terminal node in the syntax tree, derived classes are post-fixed with 'Syntax'
/// by convention.
public abstract class SyntaxNode
{
    protected IList< SyntaxToken > ChildTokens;

    protected SyntaxNode(
        IList< SyntaxToken > childTokens )
    {
        ChildTokens = childTokens;
    }
}