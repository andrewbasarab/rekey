namespace Rekey.Tests;

public class CharTypeTests
{
    [Fact]
    public void CanCheckContains()
    {
        var set = new CharTypeSet();
        set.Add(CharType.EnOrPossibleRu);
        set.Add(CharType.EnOrPossibleRu);
        set.Add(CharType.Separator);

        Assert.True(set.Contains(CharType.EnOrPossibleRu));
        Assert.True(set.Contains(CharType.Separator));
        Assert.False(set.Contains(CharType.RuOrPossibleEn));
    }

    [Fact]
    public void CanCheckContainsOnly()
    {
        var set = new CharTypeSet();
        set.Add(CharType.RuOrPossibleSeparator);

        Assert.True(set.ContainsOnly(CharType.RuOrPossibleSeparator));
        Assert.False(set.ContainsOnly(CharType.RuOrPossibleEn));

        set.Add(CharType.RuOrPossibleEn);

        Assert.False(set.ContainsOnly(CharType.RuOrPossibleSeparator));
        Assert.False(set.ContainsOnly(CharType.RuOrPossibleEn));
    }

    [Fact]
    public void CanCheckContainsOnlyForTwoCharTypes()
    {
        var set = new CharTypeSet();
        set.Add(CharType.RuOrPossibleSeparator);

        Assert.False(set.ContainsOnly(CharType.RuOrPossibleSeparator, CharType.RuOrPossibleEn));

        set.Add(CharType.RuOrPossibleEn);

        Assert.True(set.ContainsOnly(CharType.RuOrPossibleSeparator, CharType.RuOrPossibleEn));

        set.Add(CharType.Separator);

        Assert.False(set.ContainsOnly(CharType.RuOrPossibleSeparator, CharType.RuOrPossibleEn));
    }

    [Fact]
    public void CanCheckContainsOnlyFirstOrBoth()
    {
        var set = new CharTypeSet();
        set.Add(CharType.RuOrPossibleSeparator);

        Assert.True(set.ContainsOnlyFirstOrBoth(CharType.RuOrPossibleSeparator, CharType.RuOrPossibleEn));
        Assert.False(set.ContainsOnlyFirstOrBoth(CharType.RuOrPossibleEn, CharType.RuOrPossibleSeparator));

        set.Add(CharType.RuOrPossibleEn);

        Assert.True(set.ContainsOnlyFirstOrBoth(CharType.RuOrPossibleSeparator, CharType.RuOrPossibleEn));
        Assert.True(set.ContainsOnlyFirstOrBoth(CharType.RuOrPossibleEn, CharType.RuOrPossibleSeparator));
    }
}
