namespace ProtoParser.Parsing.Tokens;

internal class IdentifierToken : Token
{
    internal override required ETokenKind TokenKind { get; init; }

    internal required string Identifier { get; init; }

    public override string ToString( )
    {
        return $"Token({TokenKind}, {Identifier})";
    }
}

internal readonly ref struct Keywords
{
    internal static ReadOnlySpan< byte > Syntax => "syntax"u8;
}