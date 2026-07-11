namespace RekeyNet.Tests;

public class RekeyTests
{
    [Fact]
    public void EmptyIsEmpty()
    {
        Assert.Equal("", new Rekey().Correct(""));
    }

    // === English tests ===

    [Fact]
    public void CanDetectCorrectEnglishWords()
    {
        var rekey = new Rekey();

        Assert.Equal("beautiful", rekey.Correct("beautiful"));
        Assert.Equal("lesson", rekey.Correct("lesson"));
        Assert.Equal("bullet", rekey.Correct("bullet"));
        Assert.Equal("dark.light", rekey.Correct("dark.light"));
    }

    // === Russian tests ===

    [Fact]
    public void CanDetectCorrectRussianWords()
    {
        var rekey = new Rekey();

        Assert.Equal("источник", rekey.Correct("источник"));
        Assert.Equal("утопия", rekey.Correct("утопия"));
        Assert.Equal("взломщик", rekey.Correct("взломщик"));
        Assert.Equal("борт", rekey.Correct("борт"));
    }

    [Fact]
    public void CanDetectSwitchedEnglishWordsFromRuLayout()
    {
        var rekey = new Rekey();

        Assert.Equal("building", rekey.Correct("игшдвштп"));
        Assert.Equal("falcon", rekey.Correct("афдсщт"));
        Assert.Equal("paradise", rekey.Correct("зфкфвшыу"));
        Assert.Equal("music,bar", rekey.Correct("ьгышсбифк"));
    }

    [Fact]
    public void CanDetectSwitchedRussianWords()
    {
        var rekey = new Rekey();

        Assert.Equal("частица", rekey.Correct("xfcnbwf"));
        Assert.Equal("адрес", rekey.Correct("flhtc"));
        Assert.Equal("пирамида", rekey.Correct("gbhfvblf"));
    }

    [Fact]
    public void CanDetectSwitchedRussianWordsWithSeparators()
    {
        var rekey = new Rekey();

        Assert.Equal("бильярд", rekey.Correct(",bkmzhl"));
        Assert.Equal("любовь", rekey.Correct("k.,jdm"));
        Assert.Equal("подъезд", rekey.Correct("gjl]tpl"));
    }

    // === Ukrainian tests ===

    [Fact]
    public void CanDetectCorrectUkrainianWords()
    {
        var rekey = new Rekey();

        Assert.Equal("привіт", rekey.Correct("привіт"));
        Assert.Equal("україна", rekey.Correct("україна"));
        Assert.Equal("людина", rekey.Correct("людина"));
        Assert.Equal("сонце", rekey.Correct("сонце"));
    }

    [Fact]
    public void CanDetectSwitchedUkrainianWords()
    {
        // Ukrainian words typed in English layout
        var rekey = new Rekey();

        // "привіт" (hello) typed in English mode: ghbdsn
        Assert.Equal("привіт", rekey.Correct("ghbdsn"));

        // "людина" (person) typed in English mode: k.lbyf
        Assert.Equal("людина", rekey.Correct("k.lbyf"));

        // "сонце" (sun) typed in English mode: cjywt
        Assert.Equal("сонце", rekey.Correct("cjywt"));

        // "місто" (city) typed in English mode: vscnj
        Assert.Equal("місто", rekey.Correct("vscnj"));
    }

    [Fact]
    public void CanDetectSwitchedUkrainianWordsWithUniqueChars()
    {
        var rekey = new Rekey();

        // "їжак" (hedgehog) typed in English: ];fr
        Assert.Equal("їжак", rekey.Correct("];fr"));

        // "єдність" (unity) typed in English: 'lyscnm
        Assert.Equal("єдність", rekey.Correct("'lyscnm"));
    }

    [Fact]
    public void CanDetectEnglishTypedInUkrainianLayout()
    {
        var rekey = new Rekey();

        // "hello" typed in Ukrainian mode: рудщщ (р=h, у=e, д=l, щ=o)
        Assert.Equal("hello", rekey.Correct("руддщ"));

        // "world" typed in Ukrainian mode: цщкдв
        Assert.Equal("world", rekey.Correct("цщкдв"));

        // "music" typed in Ukrainian mode: ьгішс (і instead of ы!)
        Assert.Equal("music", rekey.Correct("ьгішс"));
    }

    [Fact]
    public void PrefersKnownWordOnRussianUkrainianAmbiguity()
    {
        // The RU/UK layouts differ only on the s ] ' ` keys, so a wrong-layout token
        // often switches to an n-gram-plausible word in both languages. The known-word
        // lists must arbitrate.
        var rekey = new Rekey();

        // "vsckm" → RU "мысль" (real word) vs UK "місль" (plausible nonsense)
        Assert.Equal("мысль", rekey.Correct("vsckm"));

        // "cskm" → UK "сіль" (real word) vs RU "сыль" (plausible nonsense)
        Assert.Equal("сіль", rekey.Correct("cskm"));
    }

    // === General tests ===

    [Fact]
    public void CanDetectDigits()
    {
        var rekey = new Rekey();

        Assert.Equal("24", rekey.Correct("24"));
        Assert.Equal("борьба24", rekey.Correct(",jhm,f24"));
        Assert.Equal("4test", rekey.Correct("4еуые"));
        Assert.Equal("life4good", rekey.Correct("дшау4пщщв"));
    }

    [Fact]
    public void CanDetectUppercaseLetters()
    {
        var rekey = new Rekey();

        Assert.Equal("БЮРО", rekey.Correct("<>HJ"));
        Assert.Equal("Бюро", rekey.Correct("<.hj"));
        Assert.Equal("Ход", rekey.Correct("{jl"));

        Assert.Equal("MAY tHe fORce BE with yoU", rekey.Correct("ЬФН еРу аЩКсу ИУ цшер нщГ"));
    }

    [Fact]
    public void LeaveAsIsIfUnknown()
    {
        var rekey = new Rekey();

        Assert.Equal("xcvn", rekey.Correct("xcvn"));
        Assert.Equal("чсмт", rekey.Correct("чсмт"));
        Assert.Equal("вбрз", rekey.Correct("вбрз"));
    }

    [Fact]
    public void LeaveAsIsIfEnAbbreviations()
    {
        var rekey = new Rekey();

        Assert.Equal("T.G.I", rekey.Correct("T.G.I"));
        Assert.Equal("r.i.p.", rekey.Correct("r.i.p."));
    }

    [Fact]
    public void CorrectedAppearsOnlyOnLangSwitch()
    {
        var rekey = new Rekey();

        Assert.NotNull(rekey.Analyze("<>HJ").Corrected);
        Assert.NotNull(rekey.Analyze("ьгыШс").Corrected);
        Assert.Null(rekey.Analyze("tEst").Corrected);
        Assert.Null(rekey.Analyze("Слово").Corrected);
        Assert.Null(rekey.Analyze("Привіт").Corrected);

        Assert.True(rekey.Analyze("<>HJ").WasCorrected);
        Assert.False(rekey.Analyze("tEst").WasCorrected);
    }
}
