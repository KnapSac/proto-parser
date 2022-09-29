namespace ProtoParser.Parsing.Tokens;

internal class StringLiteralToken : Token
{
    internal override required ETokenKind TokenKind { get; init; }

    internal required string Value { get; init; }

    public override string ToString( )
    {
        return $"Token({TokenKind}, '{Value}')";
    }
}