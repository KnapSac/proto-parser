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

            if ( token.Kind == ESyntaxKind.Syntax )
            {
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
                    Kind = ESyntaxKind.Semicolon,
                } );
            m_DiagnosticsProvider.EmitError(
                "Missing semicolon after syntax declaration.",
                nextToken );
        }

        if ( nextToken.Kind == ESyntaxKind.EndOfLine )
        {
            // Save the end of line token.
            syntaxDeclarationTokens.Add( nextToken );
            return new SyntaxDeclarationSyntax(
                syntaxDeclarationTokens,
                syntaxLevelToken );
        }

        // Skip tokens until we reach the end of the line.
        syntaxDeclarationTokens.Add( SkipUntilEndOfLine( nextToken ) );
        syntaxDeclarationTokens.Add( m_Lexer.Current );
        return new SyntaxDeclarationSyntax(
            syntaxDeclarationTokens,
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