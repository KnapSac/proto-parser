#region

using System.Text;

#endregion

namespace ProtoParser.Syntax;

public class SyntaxTree
{
    private readonly IList< SyntaxNode > m_Children;

    internal SyntaxTree( )
    {
        m_Children = new List< SyntaxNode >( );
    }

    internal void AddChild(
        SyntaxNode node )
    {
        m_Children.Add( node );
    }

    public string Source( )
    {
        StringBuilder builder = new( );
        foreach ( SyntaxNode node in m_Children )
        {
            node.Source( builder );
        }

        return builder.ToString( );
    }
}