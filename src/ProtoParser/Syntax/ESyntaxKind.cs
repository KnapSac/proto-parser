namespace ProtoParser.Syntax;

public enum ESyntaxKind : byte
{
    Identifier = 0,

    // Keywords.
    Syntax = 1,
    Import = 2,
    Weak = 3,
    Public = 4,
    Package = 5,
    Option = 6,
    Inf = 7,
    Repeated = 8,
    Optional = 9,
    Required = 10,
    Bool = 11,
    String = 12,
    Bytes = 13,
    Float = 14,
    Double = 15,
    Int32 = 16,
    Int64 = 17,
    Uint32 = 18,
    Uint64 = 19,
    Sint32 = 20,
    Sint64 = 21,
    Fixed32 = 22,
    Fixed64 = 23,
    Sfixed32 = 24,
    Sfixed64 = 25,
    Group = 26,
    Oneof = 27,
    Map = 28,
    Extensions = 29,
    To = 30,
    Max = 31,
    Reserved = 32,
    Enum = 33,
    Message = 34,
    Extend = 35,
    Service = 36,
    Rpc = 37,
    Stream = 38,
    Returns = 39,

    // Literals.
    IntLiteral = 40,
    DecimalLiteral = 41,
    OctalLiteral = 42,
    HexLiteral = 43,
    FloatLiteral = 44,
    StringLiteral = 45,

    // Punctuation and operators.
    Semicolon = 46,
    Comma = 47,
    Dot = 48,
    Slash = 49,
    Colon = 50,
    Equals = 51,
    Minus = 52,
    Plus = 53,
    LeftParen = 54,
    RightParen = 55,
    LeftBrace = 56,
    RightBrace = 57,
    LeftBracket = 58,
    RightBracket = 59,
    LeftAngle = 60,
    RightAngle = 61,

    // Trivia.
    EndOfLine = 62,
    EndOfFile = 63,
    LineComment = 64,
    BlockComment = 65,
    Whitespace = 66,
    Skipped = 67,
}

internal static class Keywords
{
    internal const string Syntax = "syntax";
    internal const string Package = "package";
    internal const string Service = "service";
    internal const string Rpc = "rpc";
    internal const string Returns = "returns";
}