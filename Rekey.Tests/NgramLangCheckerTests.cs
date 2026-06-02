namespace RekeyNet.Tests;

public class NgramLangCheckerTests
{
    [Fact]
    public void CanFindFirstVowel3Gram()
    {
        Assert.Null(NgramLangChecker.FirstNgram(Lang.En, "wasidort", 3, vowel: true));
        Assert.Equal("aou", NgramLangChecker.FirstNgram(Lang.En, "waouil", 3, vowel: true));

        Assert.Null(NgramLangChecker.FirstNgram(Lang.Ru, "бавигад", 3, vowel: true));
        Assert.Equal("ууу", NgramLangChecker.FirstNgram(Lang.Ru, "аабууу", 3, vowel: true));
    }
}
