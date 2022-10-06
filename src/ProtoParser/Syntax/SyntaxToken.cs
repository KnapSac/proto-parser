#region

using System.Text;

#endregion

namespace ProtoParser.Syntax;

/// Represents a terminal defined by the Protobuf grammar, it is the smallest possible syntactic
/// fragment of code. Can be an identifier, a keyword, a literal or punctuation.
public class SyntaxToken
{
    private IList< SyntaxTrivia > ? m_LeadingTrivia;
    private IList< SyntaxTrivia > ? m_TrailingTrivia;

    public required ESyntaxKind Kind { get; init; }
    public required string Text { get; init; }

    internal SyntaxToken WithTrivia(
        IList< SyntaxTrivia > leadingTrivia,
        IList< SyntaxTrivia > trailingTrivia )
    {
        m_LeadingTrivia = leadingTrivia;
        m_TrailingTrivia = trailingTrivia;

        return this;
    }

    public void Source(
        StringBuilder builder )
    {
        if ( null != m_LeadingTrivia )
        {
            foreach ( SyntaxTrivia trivia in m_LeadingTrivia )
            {
                builder.Append( trivia.Text );
            }
        }

        builder.Append( Text );

        if ( null != m_TrailingTrivia )
        {
            foreach ( SyntaxTrivia trivia in m_TrailingTrivia )
            {
                builder.Append( trivia.Text );
            }
        }
    }
}

/// Contains helper methods for working with `SyntaxToken`s.
internal static class SyntaxTokenFactory
{
    /// A token representing the end of the file.
    internal static readonly SyntaxTrivia EndOfFileTrivia = new( )
                                                            {
                                                                Kind = ESyntaxKind.EndOfFile,
                                                                Text = string.Empty,
                                                            };

    /// A token representing a newline.
    internal static readonly SyntaxTrivia EndOfLineTrivia = new( )
                                                            {
                                                                Kind = ESyntaxKind.EndOfLine,
                                                                // TODO: This should contain the specific newline (because it could also be \r\n).
                                                                Text = Environment.NewLine,
                                                            };

    /// A token representing a space.
    internal static readonly SyntaxTrivia Space = new( )
                                                  {
                                                      Kind = ESyntaxKind.Whitespace,
                                                      Text = " ",
                                                  };
}