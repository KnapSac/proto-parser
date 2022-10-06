#region

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
            buffer );
    }

    public static SyntaxTree Parse(
        byte[ ] buffer,
        string path )
    {
        Parser parser = new(
            buffer,
            path );
        return parser.Parse( );
    }

    private SyntaxTree Parse( )
    {
        m_Lexer.DiscardOptionalByteOrderMark( );

        SyntaxTree syntaxTree = new( );
        while ( !m_EndOfFile )
        {
            SyntaxToken token = m_Lexer.Lex( );
            if ( HandlePossibleEndOfFile(
                token,
                null ) )
            {
                return syntaxTree;
            }

            switch ( token.Kind )
            {
                case ESyntaxKind.Syntax:
                {
                    syntaxTree.AddChild( ParseSyntaxDeclaration( token ) );
                    continue;
                }
                case ESyntaxKind.Package:
                {
                    syntaxTree.AddChild( ParsePackageDeclaration( token ) );
                    continue;
                }
                case ESyntaxKind.Service:
                {
                    syntaxTree.AddChild(
                        ParseServiceDeclaration(
                            syntaxTree,
                            token ) );
                    continue;
                }
            }
        }

        return syntaxTree;
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
                    Text = string.Empty,
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
                    Text = string.Empty,
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
                    Text = string.Empty,
                } );
            m_DiagnosticsProvider.EmitError(
                "Missing semicolon after syntax declaration.",
                nextToken );
        }

        // Skip tokens until we reach the end of the line.
        if ( !m_Lexer.AtEndOfLine( ) )
        {
            syntaxDeclarationTokens.Add( SkipUntilEndOfLine( nextToken ) );
            syntaxDeclarationTokens.Add( m_Lexer.Current! );
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
                            Text = string.Empty,
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
            packageDeclarationTokens.Add( m_Lexer.Current! );
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
                        Text = string.Empty,
                    } );
                m_DiagnosticsProvider.EmitError(
                    "Missing identifier in package declaration.",
                    nextToken );
            }

            return true;
        }
    }

    /// GRAMMAR:
    /// ServiceDecl = service ServiceName l_brace { ServiceElement } r_brace .
    /// ServiceName = identifier.
    /// ServiceElement = OptionDecl |
    ///                  MethodDecl |
    ///                  EmptyDecl.
    private SyntaxNode ParseServiceDeclaration(
        SyntaxTree syntaxTree,
        SyntaxToken serviceKeywordToken )
    {
        // NOTE: Init list with capacity 4, because we expect at least 4 tokens, although there
        // might be more because the grammar allows C-style concatenation.
        IList< SyntaxToken > serviceDeclarationTokens = new List< SyntaxToken >( 4 )
                                                        {
                                                            serviceKeywordToken,
                                                        };
        SyntaxToken ? syntaxLevelToken = null;

        SyntaxToken nextToken = m_Lexer.Lex( );
        if ( nextToken.Kind == ESyntaxKind.Identifier )
        {
            // Save the identifier token.
            serviceDeclarationTokens.Add( nextToken );
            nextToken = m_Lexer.Lex( );
        }
        else
        {
            if ( HandlePossibleEndOfFile(
                nextToken,
                serviceDeclarationTokens ) )
            {
                return new ServiceDeclarationSyntax( serviceDeclarationTokens );
            }

            // Create a missing token.
            serviceDeclarationTokens.Add(
                new MissingToken
                {
                    Kind = ESyntaxKind.Identifier,
                    Text = string.Empty,
                } );
            m_DiagnosticsProvider.EmitError(
                "Missing identifier after 'service'.",
                nextToken );
        }

        if ( nextToken.Kind == ESyntaxKind.LeftBrace )
        {
            // Save the left brace token.
            serviceDeclarationTokens.Add( nextToken );
        }
        else
        {
            if ( HandlePossibleEndOfFile(
                nextToken,
                serviceDeclarationTokens ) )
            {
                return new SyntaxDeclarationSyntax( serviceDeclarationTokens );
            }

            // Create a missing token.
            serviceDeclarationTokens.Add(
                new MissingToken
                {
                    Kind = ESyntaxKind.LeftBrace,
                    Text = string.Empty,
                } );
            m_DiagnosticsProvider.EmitError(
                "Missing left brace in service declaration.",
                nextToken );
        }

        // TODO: Handle rpc declarations.
        if ( nextToken.Kind == ESyntaxKind.Rpc )
        {
        }

        if ( nextToken.Kind == ESyntaxKind.RightBrace )
        {
            // Save the right brace token.
            serviceDeclarationTokens.Add( nextToken );
        }
        else
        {
            if ( HandlePossibleEndOfFile(
                nextToken,
                serviceDeclarationTokens ) )
            {
                return new SyntaxDeclarationSyntax( serviceDeclarationTokens );
            }

            // Create a missing token.
            serviceDeclarationTokens.Add(
                new MissingToken
                {
                    Kind = ESyntaxKind.RightBrace,
                    Text = string.Empty,
                } );
            m_DiagnosticsProvider.EmitError(
                "Missing right brace in service declaration.",
                nextToken );
        }

        // Skip tokens until we reach the end of the line.
        if ( !m_Lexer.AtEndOfLine( ) )
        {
            serviceDeclarationTokens.Add( SkipUntilEndOfLine( nextToken ) );
            serviceDeclarationTokens.Add( m_Lexer.Current! );
        }

        return new SyntaxDeclarationSyntax(
            serviceDeclarationTokens,
            syntaxLevelToken );
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
                               Text = string.Empty,
                           };
                case ESyntaxKind.EndOfFile:
                    return new SkippedTokens( skippedTokens )
                           {
                               Kind = ESyntaxKind.Skipped,
                               Text = string.Empty,
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