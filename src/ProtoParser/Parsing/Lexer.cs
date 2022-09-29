#region

using System.Text;

using ProtoParser.Diagnostics;
using ProtoParser.Parsing.Tokens;

#endregion

namespace ProtoParser.Parsing;

internal class Lexer
{
    private static ReadOnlySpan< byte > ByteOrderMark => "\xEF\xBB\xBF"u8;

    private readonly byte[ ] m_Buffer;
    private readonly IList< Token > m_TokenBuffer;
    private readonly IDiagnosticsProvider m_DiagnosticsProvider;

    /// The position of the last read byte, or `-1` if no bytes have been read yet.
    private int m_Position;

    /// `true` if the lexer has seen the end of the file.
    private bool m_EndOfFile;

    private int m_Line;
    private int m_LastLineWithToken;

    internal Lexer(
        byte[ ] buffer,
        IDiagnosticsProvider diagnosticsProvider )
    {
        m_Buffer = buffer;
        // TODO: Be smarter about the initial token buffer size.
        m_TokenBuffer = new List< Token >( 512 );
        m_DiagnosticsProvider = diagnosticsProvider;

        m_Position = -1;
        m_Line = 1;
        m_LastLineWithToken = 0;
    }

    internal IList< Token > Lex( )
    {
        // Consume the byte order mark if it is present.
        if ( Peek(
                 ByteOrderMark.Length,
                 out ReadOnlySpan< byte > maybeByteOrderMark )
             && maybeByteOrderMark.SequenceEqual( ByteOrderMark ) )
        {
            Discard( ByteOrderMark.Length );
        }

        while ( !m_EndOfFile )
        {
            switch ( EatByte( ) )
            {
                // Whitespace.
                case (byte) '\n':
                {
                    // We have encountered a newline, so we need to update our state.
                    EatNewline( );
                    break;
                }
                case (byte) ' ' or (byte) '\r' or (byte) '\t' or (byte) '\f' or (byte) '\v':
                {
                    // Whitespace.
                    break;
                }

                // Comments.
                case (byte) '/':
                {
                    // Line or block comment.
                    switch ( EatByte( ) )
                    {
                        case (byte) '/':
                        {
                            // Line comment.
                            EatLineComment( );
                            break;
                        }
                        case (byte) '*':
                        {
                            // Block comment.
                            EatBlockComment( );
                            break;
                        }
                        default:
                            throw new ArgumentException( $"Unexpected byte '{(char) Current( )}'" );
                    }
                    break;
                }

                // Identifiers.
                case { } b when b == (byte) '_' || char.IsAsciiLetter( (char) b ):
                {
                    // Identifier.
                    EatIdentifier( );
                    break;
                }

                // Literals.
                case (byte) '"' or (byte) '\'':
                {
                    // String literal.
                    EatStringLiteral( );
                    break;
                }
                case { } b when char.IsAsciiDigit( (char) b ):
                {
                    // Int or float literal.
                    EatNumericLiteral( );
                    break;
                }

                // Punctuation.
                case (byte) '=':
                {
                    // Equals.
                    EmitToken(
                        new SymbolToken
                        {
                            TokenKind = ETokenKind.Equals,
                        } );
                    break;
                }
                case (byte) ';':
                {
                    // Semicolon.
                    EmitToken(
                        new SymbolToken
                        {
                            TokenKind = ETokenKind.Semicolon,
                        } );
                    break;
                }
                case (byte) '{':
                {
                    // Right brace.
                    EmitToken(
                        new SymbolToken
                        {
                            TokenKind = ETokenKind.RightBrace,
                        } );
                    break;
                }
                case (byte) '}':
                {
                    // Left brace.
                    EmitToken(
                        new SymbolToken
                        {
                            TokenKind = ETokenKind.LeftBrace,
                        } );
                    break;
                }
                case (byte) '(':
                {
                    // Right parenthesis.
                    EmitToken(
                        new SymbolToken
                        {
                            TokenKind = ETokenKind.RightParen,
                        } );
                    break;
                }
                case (byte) ')':
                {
                    // Left parenthesis.
                    EmitToken(
                        new SymbolToken
                        {
                            TokenKind = ETokenKind.LeftParen,
                        } );
                    break;
                }

                case null:
                {
                    // End of file.
                    EatEndOfFile( );
                    break;
                }
                default:
                    throw new ArgumentException( $"Unexpected byte '{(char) Current( )}'" );
            }
        }

        return m_TokenBuffer;
    }

    private void EatIdentifier( )
    {
        int startPosition = m_Position;
        while ( true )
        {
            byte ? current = EatByte( );
            if ( current is null )
            {
                EatEndOfFile( );
                return;
            }

            if ( current == (byte) '_'
                 || char.IsAsciiLetter( (char) current )
                 || char.IsAsciiDigit( (char) current ) )
            {
                // Valid identifier character, continue.
                continue;
            }

            // We have reached the end of the identifier.
            EmitToken(
                new IdentifierToken
                {
                    TokenKind = ETokenKind.Identifier,
                    Identifier = Encoding.UTF8.GetString(
                        m_Buffer,
                        startPosition,
                        m_Position - startPosition ),
                } );

            UneatByte( );
            return;
        }
    }

    private void EatStringLiteral( )
    {
        byte readUntil = Current( );
        int startPosition = m_Position + 1;

        while ( true )
        {
            byte ? current = EatByte( );
            if ( current is null )
            {
                EatEndOfFile( );
                return;
            }

            if ( (current == readUntil || current == readUntil)
                 && Previous( ) != (byte) '\\' )
            {
                EmitToken(
                    new StringLiteralToken
                    {
                        TokenKind = ETokenKind.StringLiteral,
                        Value = Encoding.UTF8.GetString(
                            m_Buffer,
                            startPosition,
                            m_Position - startPosition ),
                    } );
                return;
            }

            switch ( current )
            {
                case (byte) '\0':
                    EmitToken(
                        new InvalidToken
                        {
                            TokenKind = ETokenKind.NullCharacter,
                        } );
                    break;
                case (byte) '\n' when Previous( ) != (byte) '\\':
                    EmitToken(
                        new MissingToken
                        {
                            TokenKind = ETokenKind.Missing,
                            MissingTokenKind = ETokenKind.Quote,
                        } );
                    break;
            }
        }
    }

    private void EatNumericLiteral( )
    {
        int startPosition = m_Position;
        while ( true )
        {
            switch ( EatByte( ) )
            {
                case { } b when char.IsAsciiDigit( (char) b ):
                    break;
                case (byte) '.':
                    break;
                case (byte) 'e':
                case (byte) 'E':
                {
                    // Scientific notation.
                    switch ( EatByte( ) )
                    {
                        case (byte) '+':
                        case (byte) '-':
                            break;
                        default:
                            EmitToken(
                                new InvalidToken
                                {
                                    TokenKind = ETokenKind.FloatLiteral,
                                } );
                            return;
                    }
                    break;
                }
                default:
                    // TODO: Emit proper numeric token.
                    EmitToken(
                        new StringLiteralToken
                        {
                            TokenKind = ETokenKind.StringLiteral,
                            Value = Encoding.UTF8.GetString(
                                m_Buffer,
                                startPosition,
                                m_Position - startPosition ),
                        } );
                    return;
            }
        }
    }

    /// Consumes a line comment without consuming any whitespace before or after it. The newline
    /// after the comment is consumed.
    private void EatLineComment( )
    {
        // PRE: The leading slashes have been consumed.
        while ( true )
        {
            switch ( EatByte( ) )
            {
                case (byte) '\x00':
                {
                    EmitToken(
                        new InvalidToken
                        {
                            TokenKind = ETokenKind.NullCharacter,
                        } );

                    // Although null characters aren't allowed in comments, we continue consuming
                    // characters until we reach the end of the line, to ensure we can parse the
                    // rest of the file.
                    break;
                }
                case null:
                {
                    // We have reached the end of the file, so we're done here.
                    EatEndOfFile( );
                    return;
                }
                case (byte) '\n':
                {
                    // We have consumed a newline, so update our position before returning.
                    UpdatePositionAfterNewline( );
                    return;
                }
            }
        }
    }

    private void EatBlockComment( )
    {
        throw new NotImplementedException( );
    }

    private void EatEndOfFile( )
    {
        EmitToken(
            new SymbolToken
            {
                TokenKind = ETokenKind.EndOfFile,
            } );
        m_EndOfFile = true;
    }

    private byte ? EatByte( )
    {
        return m_Position + 1 == m_Buffer.Length
            ? null
            : m_Buffer[ ++m_Position ];
    }

    /// Sets the current position back one byte, so it can be parsed again.
    private void UneatByte( )
    {
        if ( m_Position > 0 )
        {
            --m_Position;
        }
    }

    /// Returns the last read byte. The caller is responsible for ensuring that `m_Position` is in
    /// bounds.
    private byte Current( )
    {
        return m_Buffer[ m_Position ];
    }

    private byte ? Previous( )
    {
        return m_Position <= 0
            ? null
            : m_Buffer[ m_Position - 1 ];
    }

    private void EmitToken(
        Token tokenToEmit )
    {
        m_TokenBuffer.Add( tokenToEmit );
        m_LastLineWithToken = m_Line;
    }

    /// Consumes a newline.
    private void EatNewline( )
    {
        if ( m_LastLineWithToken == m_Line )
        {
            EmitToken(
                new SymbolToken
                {
                    TokenKind = ETokenKind.EndOfLine,
                } );
        }

        UpdatePositionAfterNewline( );
    }

    /// Updates the relevant state after consuming a newline.
    private void UpdatePositionAfterNewline( )
    {
        // Update the line index and the starting position of the line.
        ++m_Line;
    }

    /// Peeks `bytesToPeek` ahead. If the return value is `true`, `bytes` contains the next
    /// `bytesToPeek` bytes. If the return value is `false`, `bytes` is empty because there are not
    /// enough bytes left to peek.
    private bool Peek(
        int bytesToPeek,
        out ReadOnlySpan< byte > bytes )
    {
        if ( m_Position + bytesToPeek >= m_Buffer.Length )
        {
            bytes = ReadOnlySpan< byte >.Empty;
            return false;
        }

        bytes = m_Buffer.AsSpan(
            m_Position + 1,
            bytesToPeek );
        return true;
    }

    /// Discards `bytesToDiscard` bytes by advancing the position by that count. The caller is
    /// responsible for ensuring that there are enough bytes left to discard.
    private void Discard(
        int bytesToDiscard )
    {
        m_Position += bytesToDiscard;
    }
}