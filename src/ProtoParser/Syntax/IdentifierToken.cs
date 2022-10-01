namespace ProtoParser.Syntax;

internal class IdentifierToken : SyntaxToken
{
    internal string Identifier => Text;

    public override string ToString( )
    {
        return $"Token({Kind}, {Identifier})";
    }
}