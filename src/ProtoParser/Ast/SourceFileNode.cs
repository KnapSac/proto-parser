namespace ProtoParser.Ast;

/// This class serves as the root of the AST. It also contains information which is defined at the
/// file level, like the syntax used.
public class SourceFileNode
{
    public SyntaxNode ? Syntax { get; set; }
}