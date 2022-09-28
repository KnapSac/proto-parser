namespace ProtoParser.Parsing.Tokens;

internal class MissingToken : Token
{
    internal override required ETokenKind TokenKind { get; init; }

    internal required ETokenKind MissingTokenKind { get; init; }
}