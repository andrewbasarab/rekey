namespace Rekey;

internal enum CharType
{
    Separator = 0b1,
    SeparatorOrPossibleRu = 0b10,
    EnOrPossibleRu = 0b100,
    RuOrPossibleEn = 0b1000,
    RuOrPossibleSeparator = 0b10000,
    Digit = 0b100000
}

internal static class CharTypeHelper
{
    public static CharType Of(char ch)
    {
        // Order is important: all about apostrophe
        if (char.IsDigit(ch))
            return CharType.Digit;

        if (Characters.IsEnOrPossibleRu(ch))
            return CharType.EnOrPossibleRu;

        if (Characters.IsSeparatorOrPossibleRu(ch))
            return CharType.SeparatorOrPossibleRu;

        if (Characters.IsRuOrPossibleSeparator(ch))
            return CharType.RuOrPossibleSeparator;

        if (Characters.IsRuOrPossibleEn(ch))
            return CharType.RuOrPossibleEn;

        return CharType.Separator;
    }
}

internal struct CharTypeSet
{
    private int _mask;

    public void Add(CharType charType)
    {
        _mask |= (int)charType;
    }

    public bool Contains(CharType charType) =>
        (_mask & (int)charType) != 0;

    public bool ContainsOnly(CharType charType) =>
        _mask == (int)charType;

    public bool ContainsOnly(CharType charType1, CharType charType2) =>
        _mask == ((int)charType1 | (int)charType2);

    public bool ContainsOnlyFirstOrBoth(CharType charType1, CharType charType2) =>
        ContainsOnly(charType1) || ContainsOnly(charType1, charType2);

    public override string ToString() =>
        Convert.ToString(_mask, 2);
}
