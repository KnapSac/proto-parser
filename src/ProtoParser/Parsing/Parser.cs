#region

using ProtoParser.Ast;
using ProtoParser.Diagnostics;
using ProtoParser.Parsing.Tokens;

#endregion

namespace ProtoParser.Parsing;

public class Parser
{
    private readonly Lexer m_Lexer;
    private readonly IDiagnosticsProvider m_DiagnosticsProvider;
    private readonly SourceFileNode m_SourceFileNode;

    private Parser(
        byte[ ] buffer,
        string path )
    {
        m_DiagnosticsProvider = new ConsoleDiagnosticsProvider( path );
        m_SourceFileNode = new SourceFileNode( );
        m_Lexer = new Lexer(
            buffer,
            m_DiagnosticsProvider );
    }

    public static void Parse(
        byte[ ] buffer,
        string path )
    {
        Parser parser = new(
            buffer,
            path );
        parser.Parse( );
    }

    private void Parse( )
    {
        m_Lexer.EatOptional( ETokenKind.ByteOrderMark );

        m_Lexer.EatComments( );

        ParseSyntaxDeclaration( );

        throw new NotImplementedException( );
    }

    private void ParseSyntaxDeclaration( )
    {
        if ( m_Lexer.EatOptional( ETokenKind.Syntax ) is null )
        {
            return;
        }

        m_DiagnosticsProvider.EmitDiagnosticIfMissingToken( m_Lexer.Eat( ETokenKind.Equals ) );

        StringLiteralToken ? syntaxLevelToken = ParseStringLiteral( );
        if ( syntaxLevelToken is not null )
        {
            m_SourceFileNode.Syntax = new SyntaxNode
                                      {
                                          Value = syntaxLevelToken.Value
                                      };
        }

        m_DiagnosticsProvider.EmitDiagnosticIfMissingToken( m_Lexer.Eat( ETokenKind.Semicolon ) );

        m_Lexer.EatComments( );
    }

    private StringLiteralToken ? ParseStringLiteral( )
    {
        Token stringLiteralToken = m_Lexer.EatStringLiteral( );
        return m_DiagnosticsProvider.EmitDiagnosticIfMissingToken( stringLiteralToken )
            ? null
            : (StringLiteralToken) stringLiteralToken;
    }
}