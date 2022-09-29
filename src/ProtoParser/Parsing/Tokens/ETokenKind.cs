namespace ProtoParser.Parsing.Tokens;

/// Enum containing all the token kinds.
internal enum ETokenKind
{
    // Special tokens
    Missing,
    EndOfFile,
    EndOfLine,
    NullCharacter,
    Identifier,

    // Literals
    StringLiteral,
    IntLiteral,
    FloatLiteral,

    // Punctuation
    Equals,
    Semicolon,
    Quote,
    LeftBrace,
    RightBrace,
    LeftParen,
    RightParen,
}