#region

using ProtoParser.Parsing.Tokens;

#endregion

namespace ProtoParser.Diagnostics;

internal class ConsoleDiagnosticsProvider : IDiagnosticsProvider
{
    private readonly string m_Path;

    internal ConsoleDiagnosticsProvider(
        string path )
    {
        m_Path = Path.GetFullPath( path );
    }

    void IDiagnosticsProvider.EmitError(
        string message,
        Token ? token )
    {
        if ( token is not null )
        {
            ((IDiagnosticsProvider) this).EmitError(
                message,
                token.Line,
                token.Column );
            return;
        }

        Console.Error.WriteLine( $"error: {message}" );
    }

    void IDiagnosticsProvider.EmitError(
        string message,
        int line,
        int column )
    {
        Console.Error.WriteLine( $"{m_Path}({line},{column}): error: {message}" );
    }

    bool IDiagnosticsProvider.EmitDiagnosticIfMissingToken(
        Token token )
    {
        if ( token is not MissingToken missingToken )
        {
            return false;
        }

        throw new NotImplementedException( );
    }
}