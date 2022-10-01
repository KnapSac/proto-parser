namespace ProtoParser.Syntax;

internal class StringLiteralToken : SyntaxToken
{
    public override string ToString( )
    {
        return $"Token({Kind}, '{Text}')";
    }
}