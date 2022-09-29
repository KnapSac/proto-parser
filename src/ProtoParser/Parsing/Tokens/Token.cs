namespace ProtoParser.Parsing.Tokens;

/// Abstract base class from which all token kinds are derived.
internal abstract class Token
{
    /// The kind of the token.
    internal abstract required ETokenKind TokenKind { get; init; }

    internal int Line { get; init; }
    internal int Column { get; init; }

    public override string ToString( )
    {
        return $"Token({TokenKind})";
    }
}