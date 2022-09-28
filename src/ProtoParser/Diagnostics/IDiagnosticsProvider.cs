#region

using ProtoParser.Parsing.Tokens;

#endregion

namespace ProtoParser.Diagnostics;

internal interface IDiagnosticsProvider
{
    void EmitError(
        string message,
        Token ? token = null );

    void EmitError(
        string message,
        int line = 0,
        int column = 0 );

    bool EmitDiagnosticIfMissingToken(
        Token token );
}