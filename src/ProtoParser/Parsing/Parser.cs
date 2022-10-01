#region

using ProtoParser.Ast;
using ProtoParser.Diagnostics;
using ProtoParser.Syntax;

#endregion

namespace ProtoParser.Parsing;

public class Parser
{
    private readonly IDiagnosticsProvider m_DiagnosticsProvider;
    private readonly Lexer m_Lexer;

    private bool m_EndOfFile;

    private Parser(
        byte[ ] buffer,
        string path )
    {
        m_DiagnosticsProvider = new ConsoleDiagnosticsProvider( path );
        m_Lexer = new Lexer(
            buffer,
            m_DiagnosticsProvider );
    }

    public static FileNode Parse(
        byte[ ] buffer,
        string path )
    {
        Parser parser = new(
            buffer,
            path );
        return parser.Parse( );
    }

    private FileNode Parse( )
    {
        m_Lexer.DiscardOptionalByteOrderMark( );

        bool isInitialSyntaxToken = true;
        bool isInitialPackageToken = true;
        FileNode fileNode = new( );
        while ( !m_EndOfFile )
        {
            SyntaxToken token = m_Lexer.Lex( );
            if ( HandlePossibleEndOfFile(
                token,
                null ) )
            {
                return fileNode;
            }

            switch ( token.Kind )
            {
                case ESyntaxKind.Syntax:
                {
                    // TODO: Ensure that syntax is the first token in the file, if present.
                    if ( !isInitialSyntaxToken )
                    {
                        m_DiagnosticsProvider.EmitError(
                            $"Multiple syntax declarations not allowed, originally declared at {fileNode.SyntaxDeclaration!}.",
                            token );
                        continue;
                    }

                    isInitialSyntaxToken = false;
                    fileNode.SyntaxDeclaration = ParseSyntaxDeclaration( token );
                    continue;
                }
                case ESyntaxKind.Package:
                {
                    if ( !isInitialPackageToken )
                    {
                        m_DiagnosticsProvider.EmitError(
                            $"Multiple package declarations not allowed, originally declared at {fileNode.PackageDeclaration!}.",
                            token );
                        continue;
                    }

                    isInitialPackageToken = false;
                    fileNode.PackageDeclaration = ParsePackageDeclaration( token );
                    continue;
                }
            }
        }

        return fileNode;
    }

    /// GRAMMAR:
    /// SyntaxDecl  = syntax equals SyntaxLevel semicolon .
    /// SyntaxLevel = StringLiteral .
    private SyntaxDeclarationSyntax ParseSyntaxDeclaration(
        SyntaxToken syntaxKeywordToken )
    {
        // NOTE: Init list with capacity 4, because we expect at least 4 tokens, although there
        // might be more because the grammar allows C-style concatenation.
        IList< SyntaxToken > syntaxDeclarationTokens = new List< SyntaxToken >( 4 )
                                                       {
                                                           syntaxKeywordToken,
                                                       };
        SyntaxToken ? syntaxLevelToken = null;

        SyntaxToken nextToken = m_Lexer.Lex( );
        if ( nextToken.Kind == ESyntaxKind.Equals )
        {
            // Save the equals token.
            syntaxDeclarationTokens.Add( nextToken );
            nextToken = m_Lexer.Lex( );
        }
        else
        {
            if ( HandlePossibleEndOfFile(
                nextToken,
                syntaxDeclarationTokens ) )
            {
                return new SyntaxDeclarationSyntax( syntaxDeclarationTokens );
            }

            // Create a missing token.
            syntaxDeclarationTokens.Add(
                new MissingToken
                {
                    Kind = ESyntaxKind.Equals,
                } );
            m_DiagnosticsProvider.EmitError(
                "Missing '=' after 'syntax'.",
                nextToken );
        }

        // TODO: Handle C-style string literal concatenation.
        if ( nextToken.Kind == ESyntaxKind.StringLiteral )
        {
            // Save the string literal token.
            syntaxDeclarationTokens.Add( nextToken );
            syntaxLevelToken = nextToken;
            nextToken = m_Lexer.Lex( );
        }
        else
        {
            if ( HandlePossibleEndOfFile(
                nextToken,
                syntaxDeclarationTokens ) )
            {
                return new SyntaxDeclarationSyntax( syntaxDeclarationTokens );
            }

            // Create a missing token.
            syntaxDeclarationTokens.Add(
                new MissingToken
                {
                    Kind = ESyntaxKind.StringLiteral,
                } );
            m_DiagnosticsProvider.EmitError(
                "Missing syntax level for syntax declaration.",
                nextToken );
        }

        if ( nextToken.Kind == ESyntaxKind.Semicolon )
        {
            // Save the semicolon token.
            syntaxDeclarationTokens.Add( nextToken );
        }
        else
        {
            if ( HandlePossibleEndOfFile(
                nextToken,
                syntaxDeclarationTokens ) )
            {
                return new SyntaxDeclarationSyntax( syntaxDeclarationTokens );
            }

            // Create a missing token.
            syntaxDeclarationTokens.Add(
                new MissingToken
                {
                    Kind = ESyntaxKind.Semicolon,
                } );
            m_DiagnosticsProvider.EmitError(
                "Missing semicolon after syntax declaration.",
                nextToken );
        }

        // Skip tokens until we reach the end of the line.
        if ( !m_Lexer.AtEndOfLine( ) )
        {
            syntaxDeclarationTokens.Add( SkipUntilEndOfLine( nextToken ) );
            syntaxDeclarationTokens.Add( m_Lexer.Current );
        }

        return new SyntaxDeclarationSyntax(
            syntaxDeclarationTokens,
            syntaxLevelToken );
    }

    /// GRAMMAR:
    /// PackageDecl = package PackageName semicolon .
    /// PackageName = QualifiedIdentifier .
    /// QualifiedIdentifier = identifier { dot identifier } .
    private PackageDeclarationSyntax ParsePackageDeclaration(
        SyntaxToken packageKeywordToken )
    {
        // NOTE: Init list with capacity 3, because we expect at least 3 tokens, although there
        // might be more because the grammar allows C-style concatenation.
        IList< SyntaxToken > packageDeclarationTokens = new List< SyntaxToken >( 3 )
                                                        {
                                                            packageKeywordToken,
                                                        };
        SyntaxToken nextToken = m_Lexer.Lex( );
        if ( !ParseIdentifier( ) )
        {
            return new PackageDeclarationSyntax( packageDeclarationTokens );
        }

        bool identifierFinished = false;
        while ( !identifierFinished )
        {
            switch ( nextToken.Kind )
            {
                case ESyntaxKind.Semicolon:
                {
                    // Save the semicolon token.
                    packageDeclarationTokens.Add( nextToken );
                    identifierFinished = true;
                    break;
                }
                case ESyntaxKind.Dot:
                {
                    // Save the dot token.
                    packageDeclarationTokens.Add( nextToken );
                    nextToken = m_Lexer.Lex( );
                    if ( !ParseIdentifier( ) )
                    {
                        return new PackageDeclarationSyntax( packageDeclarationTokens );
                    }
                    break;
                }
                default:
                {
                    if ( HandlePossibleEndOfFile(
                        nextToken,
                        packageDeclarationTokens ) )
                    {
                        return new PackageDeclarationSyntax( packageDeclarationTokens );
                    }

                    // Create a missing token.
                    packageDeclarationTokens.Add(
                        new MissingToken
                        {
                            Kind = ESyntaxKind.Semicolon,
                        } );
                    m_DiagnosticsProvider.EmitError(
                        "Missing semicolon after package declaration.",
                        nextToken );
                    identifierFinished = true;
                    break;
                }
            }
        }

        // Skip tokens until we reach the end of the line.
        if ( !m_Lexer.AtEndOfLine( ) )
        {
            packageDeclarationTokens.Add( SkipUntilEndOfLine( nextToken ) );
            packageDeclarationTokens.Add( m_Lexer.Current );
        }

        return new PackageDeclarationSyntax( packageDeclarationTokens );

        bool ParseIdentifier( )
        {
            if ( nextToken.Kind == ESyntaxKind.Identifier )
            {
                // Save the identifier token.
                packageDeclarationTokens.Add( nextToken );
                nextToken = m_Lexer.Lex( );
            }
            else
            {
                if ( HandlePossibleEndOfFile(
                    nextToken,
                    packageDeclarationTokens ) )
                {
                    return false;
                }

                // Create a missing token.
                packageDeclarationTokens.Add(
                    new MissingToken
                    {
                        Kind = ESyntaxKind.Identifier,
                    } );
                m_DiagnosticsProvider.EmitError(
                    "Missing identifier in package declaration.",
                    nextToken );
            }

            return true;
        }
    }

    /// Skips tokens until either an end of line or end of file token. The skipped tokens are stored
    /// as the children of the `SkippedToken`.
    private SkippedTokens SkipUntilEndOfLine(
        SyntaxToken ? alreadySkippedToken )
    {
        IList< SyntaxToken > skippedTokens = new List< SyntaxToken >( );
        if ( alreadySkippedToken is not null )
        {
            skippedTokens.Add( alreadySkippedToken );
        }

        while ( true )
        {
            SyntaxToken token = m_Lexer.Lex( );
            switch ( token.Kind )
            {
                case ESyntaxKind.EndOfLine:
                    return new SkippedTokens( skippedTokens )
                           {
                               Kind = ESyntaxKind.Skipped,
                           };
                case ESyntaxKind.EndOfFile:
                    return new SkippedTokens( skippedTokens )
                           {
                               Kind = ESyntaxKind.Skipped,
                           };
                default:
                    skippedTokens.Add( token );
                    continue;
            }
        }
    }

    /// Handles a possible end of file token. If `tokenToCheck` is an end of file token, and
    /// `tokenList` is not null, an end of file token is added to `tokenList`.
    private bool HandlePossibleEndOfFile(
        SyntaxToken tokenToCheck,
        IList< SyntaxToken > ? tokenList )
    {
        if ( tokenToCheck.Kind != ESyntaxKind.EndOfFile )
        {
            return false;
        }

        m_EndOfFile = true;
        m_DiagnosticsProvider.EmitError(
            "Unexpected end of file.",
            tokenToCheck );
        tokenList?.Add( tokenToCheck );
        return true;
    }
}