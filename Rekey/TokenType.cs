namespace RekeyNet;

public enum TokenType
{
    Separator,
    Word
}

internal static class TokenTypeExtensions
{
    public static TokenType Of(bool isSeparator) =>
        isSeparator ? TokenType.Separator : TokenType.Word;
}
