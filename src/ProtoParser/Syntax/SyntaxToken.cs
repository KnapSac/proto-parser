namespace ProtoParser.Syntax;

/// Represents a terminal defined by the Protobuf grammar, it is the smallest possible syntactic
/// fragment of code. Can be an identifier, a keyword, a literal or punctuation.
public class SyntaxToken
{
    private IList< SyntaxTrivia > ? m_LeadingTrivia;
    private IList< SyntaxTrivia > ? m_TrailingTrivia;

    public required ESyntaxKind Kind { get; init; }
    public string Text { get; init; }

    internal void WithTrivia(
        IList< SyntaxTrivia > leadingTrivia,
        IList< SyntaxTrivia > trailingTrivia )
    {
        m_LeadingTrivia = leadingTrivia;
        m_TrailingTrivia = trailingTrivia;
    }
}

/// Contains helper methods for working with `SyntaxToken`s.
internal static class SyntaxTokenFactory
{
    /// A token representing the end of the file.
    internal static readonly SyntaxTrivia EndOfFileTrivia = new( )
                                                            {
                                                                Kind = ESyntaxKind.EndOfFile,
                                                            };

    /// A token representing a newline.
    internal static readonly SyntaxTrivia EndOfLineTrivia = new( )
                                                            {
                                                                Kind = ESyntaxKind.EndOfLine,
                                                            };

    /// A token representing a space.
    internal static readonly SyntaxTrivia Space = new( )
                                                  {
                                                      Kind = ESyntaxKind.Whitespace,
                                                      Text = " ",
                                                  };
}