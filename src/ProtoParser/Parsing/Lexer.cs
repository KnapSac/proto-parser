#region

using System.Text;

using ProtoParser.Diagnostics;
using ProtoParser.Parsing.Tokens;

#endregion

namespace ProtoParser.Parsing;

internal class Lexer
{
    private static ReadOnlySpan< byte > ByteOrderMark => new(
        new[ ]
        {
            (byte) '\xEF',
            (byte) '\xBB',
            (byte) '\xBF',
        },
        0,
        3 );

    private readonly byte[ ] m_Buffer;
    private readonly IDiagnosticsProvider m_DiagnosticsProvider;

    private int m_Position;
    private int m_Line;
    private int m_LineStartPosition;

    internal Lexer(
        byte[ ] buffer,
        IDiagnosticsProvider diagnosticsProvider )
    {
        m_Buffer = buffer;
        m_DiagnosticsProvider = diagnosticsProvider;

        m_Position = -1;
        m_Line = 1;
        m_LineStartPosition = 0;
    }

    internal Token Eat(
        ETokenKind tokenKind )
    {
        EatWhitespace( );

        switch ( tokenKind )
        {
            case ETokenKind.Equals:
            {
                if ( Peek( ) != (byte) '=' )
                {
                    return new MissingToken
                           {
                               TokenKind = ETokenKind.Missing,
                               MissingTokenKind = ETokenKind.Equals,
                           };
                }

                EatByte( );
                return new SymbolToken
                       {
                           TokenKind = ETokenKind.Equals,
                       };
            }
            case ETokenKind.Semicolon:
            {
                if ( Peek( ) != (byte) ';' )
                {
                    return new MissingToken
                           {
                               TokenKind = ETokenKind.Missing,
                               MissingTokenKind = ETokenKind.Semicolon,
                           };
                }

                EatByte( );
                return new SymbolToken
                       {
                           TokenKind = ETokenKind.Semicolon,
                       };
            }
            default:
                throw new ArgumentOutOfRangeException(
                    nameof( tokenKind ),
                    tokenKind,
                    "Unexpected token" );
        }
    }

    internal Token ? EatOptional(
        ETokenKind tokenKind )
    {
        switch ( tokenKind )
        {
            case ETokenKind.ByteOrderMark:
            {
                if ( Peek( ) == ByteOrderMark[ 0 ]
                     && Peek( 2 ) == ByteOrderMark[ 1 ]
                     && Peek( 3 ) == ByteOrderMark[ 2 ] )
                {
                    EatBytes( ByteOrderMark.Length );
                    return new SymbolToken
                           {
                               TokenKind = ETokenKind.ByteOrderMark,
                           };
                }

                return null;
            }
            case ETokenKind.Syntax:
                // TODO: There is probably a better way to do this.
                int idx = 0;
                foreach ( byte b in Keywords.Syntax )
                {
                    if ( Peek( ++idx ) != b )
                    {
                        return new MissingToken
                               {
                                   TokenKind = ETokenKind.Missing,
                                   MissingTokenKind = ETokenKind.Syntax,
                               };
                    }
                }

                EatBytes( Keywords.Syntax.Length );
                return new KeywordToken
                       {
                           TokenKind = ETokenKind.Syntax,
                       };
            default:
                throw new ArgumentOutOfRangeException(
                    nameof( tokenKind ),
                    tokenKind,
                    "Unexpected token" );
        }
    }

    internal Token EatStringLiteral( )
    {
        // PERF: There is room for improvement here, relying on strings is probably not the smartest
        // idea. Concatenation is possible, but probably won't happen too often.
        string literalValue = string.Empty;
        int literalValueStartPosition = 0;
        while ( true )
        {
            EatWhitespace( );

            byte ? peeked = Peek( );
            if ( peeked is not ((byte) '\"' or (byte) '\'') )
            {
                return string.IsNullOrEmpty( literalValue )
                    ? new MissingToken
                      {
                          TokenKind = ETokenKind.Missing,
                          MissingTokenKind = ETokenKind.StringLiteral,
                      }
                    : new StringLiteralToken
                      {
                          TokenKind = ETokenKind.StringLiteral,
                          Value = literalValue,
                          Line = m_Line,
                          Column = literalValueStartPosition - m_LineStartPosition - 1,
                      };
            }

            EatByte( );

            // We don't want to include the delimiter in the literal value, so we need to skip an
            // additional position.
            int startPosition = m_Position + 1;

            byte readUntil = peeked.Value;
            while ( true )
            {
                if ( Peek( ) == readUntil
                     && m_Buffer[ m_Position ] != (byte) '\\' )
                {
                    EatByte( );
                    break;
                }

                EatByte( );
            }

            if ( string.IsNullOrEmpty( literalValue ) )
            {
                literalValueStartPosition = startPosition;
            }

            literalValue += Encoding.UTF8.GetString(
                m_Buffer,
                startPosition,
                m_Position - startPosition );
        }
    }

    /// Consumes line and block comments, with any whitespace before and after it. Also consumes the
    /// newline after a line comment!
    internal void EatComments( )
    {
        // We need to handle comments separated by empty lines, so continue to parse until we can
        // find no more comments.
        while ( true )
        {
            // Consume any whitespace which occurs before the comment.
            EatWhitespace( );

            byte ? peeked = Peek( );
            if ( peeked is not (byte) '/' )
            {
                // Either we have reached the end of the file, or there is no comment here.
                return;
            }

            // Consume the peeked byte, and peek another one.
            EatByte( );
            peeked = Peek( );

            // Abort if we have found a malformed comment.
            if ( peeked is not ((byte) '/' or (byte) '*') )
            {
                m_DiagnosticsProvider.EmitError(
                    "Comments should start with '//' or '/*'",
                    m_Line,
                    m_Position - m_LineStartPosition );
                return;
            }

            // Consume the second byte of the comment indicator.
            EatByte( );

            // Parse the appropriate comment type.
            if ( peeked == '/' )
            {
                EatLineComment( );
            }
            else
            {
                EatBlockComment( );
            }

            // Consume any whitespace which occurs after the comment.
            EatWhitespace( );
        }
    }

    /// Consumes all available whitespace. Will also consume newlines!
    private void EatWhitespace( )
    {
        while ( true )
        {
            switch ( Peek( ) )
            {
                case (byte) ' ':
                case (byte) '\r':
                case (byte) '\t':
                case (byte) '\f':
                case (byte) '\v':
                    EatByte( );
                    continue;
                case (byte) '\n':
                    EatNewline( );
                    continue;
                default:
                    // No more whitespace to consume, so we're finished.
                    return;
            }
        }
    }

    /// Consumes a line comment without consuming any whitespace before or after it.
    private void EatLineComment( )
    {
        // PRE: The leading slashes have been consumed.
        while ( true )
        {
            switch ( Peek( ) )
            {
                case (byte) '\x00':
                    m_DiagnosticsProvider.EmitError(
                        "Comments are not allowed to contain the null character",
                        m_Line,
                        m_Position - m_LineStartPosition );

                    // Although null characters aren't allowed in comments, we continue consuming
                    // characters until we reach the end of the line, to ensure we can parse the
                    // rest of the file.
                    EatByte( );
                    continue;
                case (byte) '\n':
                    // We have reached the end of the line, consuming the newline is the
                    // responsibility of the caller.
                    return;
                default:
                    EatByte( );
                    continue;
            }
        }
    }

    private void EatBlockComment( )
    {
        throw new NotImplementedException( );
    }

    /// Consumes a newline and updates the relevant state.
    private void EatNewline( )
    {
        // Consume the newline.
        EatByte( );

        // Update the line index and the starting position of the line.
        ++m_Line;
        m_LineStartPosition = m_Position;
    }

    private void EatByte( )
    {
        ++m_Position;
    }

    private void EatBytes(
        int count )
    {
        m_Position += count;
    }

    private byte ? Peek(
        int lookAhead = 1 )
    {
        return m_Position + lookAhead >= m_Buffer.Length
            ? null
            : m_Buffer[ m_Position + lookAhead ];
    }
}