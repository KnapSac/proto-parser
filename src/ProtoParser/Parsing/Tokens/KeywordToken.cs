namespace ProtoParser.Parsing.Tokens;

internal class KeywordToken : Token
{
    internal override required ETokenKind TokenKind { get; init; }
}

internal readonly ref struct Keywords
{
    internal static ReadOnlySpan< byte > Syntax => "syntax"u8;
}