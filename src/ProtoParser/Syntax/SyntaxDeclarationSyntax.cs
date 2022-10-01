#region

using ProtoParser.Diagnostics;

#endregion

namespace ProtoParser.Syntax;

/// Represents the syntax level.
public enum ESyntaxLevel : byte
{
    Missing = 0,
    Proto2 = 2,
    Proto3 = 3,
}

/// Represents a syntax declaration. Default `SyntaxLevel` is 2.
public class SyntaxDeclarationSyntax : SyntaxNode
{
    private const string PROTO_2 = "\"proto2\"";
    private const string PROTO_3 = "\"proto3\"";

    /// The syntax level declared. Default `SyntaxLevel` is 2, if parsing failed to parse the syntax
    /// level, `SyntaxLevel` is `Missing`.
    public ESyntaxLevel SyntaxLevel { get; }

    internal SyntaxDeclarationSyntax(
        IList< SyntaxToken > childTokens )
        : base( childTokens )
    {
    }

    internal SyntaxDeclarationSyntax(
        IList< SyntaxToken > childTokens,
        SyntaxToken ? syntaxLevelToken,
        IDiagnosticsProvider ? diagnosticsProvider = null )
        : base( childTokens )
    {
        if ( syntaxLevelToken is null )
        {
            // Syntax level was missing from declaration, or we failed to parse it.
            SyntaxLevel = ESyntaxLevel.Missing;
            return;
        }

        SyntaxLevel = ESyntaxLevel.Proto2;

        switch ( syntaxLevelToken.Text )
        {
            case PROTO_2:
                SyntaxLevel = ESyntaxLevel.Proto2;
                break;
            case PROTO_3:
                SyntaxLevel = ESyntaxLevel.Proto3;
                break;
            case null:
                break;
            default:
                diagnosticsProvider?.EmitError(
                    $"Invalid syntax level {syntaxLevelToken.Text}. Supported values are '{PROTO_2}' and '{PROTO_3}'.",
                    syntaxLevelToken );
                break;
        }
    }
}