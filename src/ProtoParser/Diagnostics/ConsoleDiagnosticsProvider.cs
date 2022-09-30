#region

using ProtoParser.Syntax;

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
        SyntaxToken token )
    {
        // TODO: Include span info of token in output
        Console.Error.WriteLine( $"{m_Path}(?,?): error: {message}" );
    }
}