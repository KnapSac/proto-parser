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
        foreach ( Token token in m_Lexer.Lex( ) )
        {
            Console.WriteLine( token );
        }
    }
}