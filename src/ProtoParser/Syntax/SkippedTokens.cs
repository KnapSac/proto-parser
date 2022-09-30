namespace ProtoParser.Syntax;

public class SkippedTokens : SyntaxToken
{
    private readonly IList< SyntaxToken > m_SkippedTokens;

    internal SkippedTokens(
        IList< SyntaxToken > skippedTokens )
    {
        m_SkippedTokens = skippedTokens;
    }
}