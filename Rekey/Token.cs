namespace RekeyNet;

internal sealed class Token
{
    public TokenType Type { get; }
    public string Original { get; }
    public string Canonical { get; }
    public string Corrected { get; }
    public CharTypeSet CharTypes { get; }

    /// <summary>How certain the correction of this token is; 1.0 when unchanged.</summary>
    public double Confidence { get; }

    public Token(TokenType type, string original, string canonical, string corrected,
        CharTypeSet charTypes = default, double confidence = 1.0)
    {
        Type = type;
        Original = original;
        Canonical = canonical;
        Corrected = corrected;
        CharTypes = charTypes;
        Confidence = confidence;
    }

    public bool IsWord => Type == TokenType.Word;

    public override string ToString() => $"{Type}:'{Original}'";
}
