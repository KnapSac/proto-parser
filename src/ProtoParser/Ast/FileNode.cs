#region

using ProtoParser.Syntax;

#endregion

namespace ProtoParser.Ast;

/// This class serves as the root of the AST. It also contains information which is defined at the
/// file level, like the syntax used.
public sealed class FileNode
{
    public SyntaxDeclarationSyntax ? SyntaxDeclaration { get; internal set; }

    public ESyntaxLevel SyntaxLevel => SyntaxDeclaration?.SyntaxLevel ?? ESyntaxLevel.Proto2;

    internal FileNode( )
    {
    }
}