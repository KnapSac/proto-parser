#region

using ProtoParser.Syntax;

#endregion

namespace ProtoParser.Diagnostics;

internal interface IDiagnosticsProvider
{
    void EmitError(
        string message,
        SyntaxToken token );
}