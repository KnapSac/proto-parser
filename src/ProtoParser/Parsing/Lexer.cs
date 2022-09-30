﻿#region

using System.Text;

using ProtoParser.Diagnostics;
using ProtoParser.Parsing.Tokens;
using ProtoParser.Syntax;

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

    private IList< SyntaxTrivia > m_LeadingTrivia;
    private IList< SyntaxTrivia > m_TrailingTrivia;
    private IList< SyntaxTrivia > m_CurrentTrivia;

    internal SyntaxToken Current { get; private set; }

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

    /// Discards the byte order mark, if it was present.
    internal void DiscardOptionalByteOrderMark( )
    {
        // Consume the byte order mark if it is present.
        if ( Peek(
                 ByteOrderMark.Length,
                 out ReadOnlySpan< byte > maybeByteOrderMark )
             && maybeByteOrderMark.SequenceEqual( ByteOrderMark ) )
        {
            Discard( ByteOrderMark.Length );
        }
    }

    internal SyntaxToken Lex( )
    {
        LexTrivia( false );

        while ( true )
        {
            byte ? next = Peek( );
            if ( next is null )
            {
                // We have reached the end of the file.
                EatEndOfFile( );
                return Current;
            }

            EatByte( );
            switch ( next )
            {
                case (byte) ' ':
                case (byte) '\n':
                case (byte) '\r':
                case (byte) '\t':
                case (byte) '\f':
                case (byte) '\v':
                case (byte) '\\':
                {
                    // Trailing trivia.
                    LexTrivia( true );
                    return Current;
                }
            }
        }
    }

    private void LexTrivia(
        bool trailing )
    {
        if ( trailing )
        {
            m_TrailingTrivia = new List< SyntaxTrivia >( );
            m_CurrentTrivia = m_TrailingTrivia;
        }
        else
        {
            m_LeadingTrivia = new List< SyntaxTrivia >( );
            m_CurrentTrivia = m_LeadingTrivia;
        }

        while ( true )
        {
            byte ? next = Peek( );
            if ( next is null )
            {
                // We have reached the end of the file.
                EatEndOfFile( );
                return;
            }

            switch ( next )
            {
                // Whitespace.
                case (byte) '\n':
                {
                    // We have encountered a newline, so we need to update our state.
                    EatByte( );
                    EatNewline( );

                    if ( trailing )
                    {
                        // Trailing trivia runs till the end of the line.
                        return;
                    }

                    break;
                }
                case (byte) ' ' or (byte) '\r' or (byte) '\t' or (byte) '\f' or (byte) '\v':
                {
                    // Whitespace.
                    EatByte( );
                    EatWhitespace( );
                    break;
                }

                // Comments.
                case (byte) '\\':
                {
                    // Line or block comment.
                    EatByte( );

                    switch ( Peek( ) )
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
                            throw new ArgumentException(
                                $"Unexpected byte '{(char) CurrentByte( )}'" );
                    }
                    break;
                }

                default:
                    EatEndOfFile( );
                    return;
            }
        }
    }

    /// Adds end of file trivia to the current trailing trivia, and sets `m_EndOfFile` to true.
    private void EatEndOfFile( )
    {
        m_EndOfFile = true;
        m_CurrentTrivia.Add( SyntaxTokenFactory.EndOfFileTrivia );
        Current = new SyntaxToken
                  {
                      Kind = ESyntaxKind.EndOfFile,
                  };
        Current.WithTrivia(
            m_LeadingTrivia,
            m_TrailingTrivia );
    }

    /// Adds newline trivia to the current trailing trivia, and updates the line index.
    private void EatNewline( )
    {
        m_CurrentTrivia.Add( SyntaxTokenFactory.EndOfLineTrivia );
        ++m_Line;
    }

    private void EatWhitespace( )
    {
        int startPosition = m_Position;
        while ( true )
        {
            byte ? next = Peek( );
            switch ( next )
            {
                case null:
                    EatEndOfFile( );
                    return;
                case (byte) ' ' or (byte) '\r' or (byte) '\t' or (byte) '\f' or (byte) '\v':
                    EatByte( );
                    continue;
            }

            break;
        }

        if ( m_Position - startPosition == 1
             && CurrentByte( ) == (byte) ' ' )
        {
            m_CurrentTrivia.Add( SyntaxTokenFactory.Space );
            return;
        }

        m_CurrentTrivia.Add(
            new SyntaxTrivia
            {
                Kind = ESyntaxKind.Whitespace,
                Text = Encoding.UTF8.GetString(
                    m_Buffer,
                    startPosition,
                    m_Position - startPosition ),
            } );
    }

    /*
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
    */

    /// Consumes a line comment without consuming any whitespace before it. The newline after the
    /// comment is *not* consumed.
    private void EatLineComment( )
    {
        // PRE: The leading slashes have been consumed.
        while ( true )
        {
            byte ? next = Peek( );
            switch ( next )
            {
                case (byte) '\x00':
                {
                    // TODO: Add invalid token trivia.

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
                    // We have reached the end of the line comment, the caller is responsible for
                    // consuming the newline.
                    return;
                }
            }

            EatByte( );
        }
    }

    private void EatBlockComment( )
    {
        throw new NotImplementedException( );
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

    /// Returns the next byte to be read, or `null` if the end of the file has been reached.
    private byte ? Peek( )
    {
        return m_Position + 1 >= m_Buffer.Length
            ? null
            : m_Buffer[ m_Position + 1 ];
    }

    /// Returns the next byte, the caller is responsible for ensuring that the read is in bounds!
    private byte EatByte( )
    {
        return m_Buffer[ ++m_Position ];
    }

    /// Returns the last read byte.
    private byte CurrentByte( )
    {
        return m_Buffer[ m_Position ];
    }
}