namespace Rekey;

internal sealed class Token
{
    public TokenType Type { get; }
    public string Original { get; }
    public string Canonical { get; }
    public string Corrected { get; }
    public CharTypeSet CharTypes { get; }

    public Token(TokenType type, string original, string canonical, string corrected, CharTypeSet charTypes = default)
    {
        Type = type;
        Original = original;
        Canonical = canonical;
        Corrected = corrected;
        CharTypes = charTypes;
    }

    public bool IsWord => Type == TokenType.Word;

    public override string ToString() => $"{Type}:'{Original}'";
}
