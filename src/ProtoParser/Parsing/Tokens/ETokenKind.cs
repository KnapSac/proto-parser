namespace ProtoParser.Parsing.Tokens;

/// Enum containing all the token kinds.
internal enum ETokenKind
{
    // Special tokens
    Missing,
    EndOfFile,
    ByteOrderMark,

    // Keywords
    Syntax,

    // Literals
    StringLiteral,

    // Punctuation
    Equals,
    Semicolon,
}