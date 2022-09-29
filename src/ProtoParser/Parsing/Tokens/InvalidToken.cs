namespace ProtoParser.Parsing.Tokens;

internal class InvalidToken : Token
{
    internal override required ETokenKind TokenKind { get; init; }
}