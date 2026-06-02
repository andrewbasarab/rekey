namespace Rekey.Tests;

public class RekeyTokenizerTests
{
    [Fact]
    public void EmptyIsEmpty()
    {
        Assert.Equal("", RekeyTokenizer.Create().Tokenize("").ToString());
    }

    // === English tests ===

    [Fact]
    public void CanDetectCorrectEnglishWords()
    {
        var tokenizer = RekeyTokenizer.Create();

        Assert.Equal("beautiful", tokenizer.Tokenize("beautiful").ToString());
        Assert.Equal("lesson", tokenizer.Tokenize("lesson").ToString());
        Assert.Equal("bullet", tokenizer.Tokenize("bullet").ToString());
        Assert.Equal("dark.light", tokenizer.Tokenize("dark.light").ToString());
    }

    // === Russian tests ===

    [Fact]
    public void CanDetectCorrectRussianWords()
    {
        var tokenizer = RekeyTokenizer.Create();

        Assert.Equal("источник", tokenizer.Tokenize("источник").ToString());
        Assert.Equal("утопия", tokenizer.Tokenize("утопия").ToString());
        Assert.Equal("взломщик", tokenizer.Tokenize("взломщик").ToString());
        Assert.Equal("борт", tokenizer.Tokenize("борт").ToString());
    }

    [Fact]
    public void CanDetectSwitchedEnglishWordsFromRuLayout()
    {
        var tokenizer = RekeyTokenizer.Create();

        Assert.Equal("building", tokenizer.Tokenize("игшдвштп").ToString());
        Assert.Equal("falcon", tokenizer.Tokenize("афдсщт").ToString());
        Assert.Equal("paradise", tokenizer.Tokenize("зфкфвшыу").ToString());
        Assert.Equal("music,bar", tokenizer.Tokenize("ьгышсбифк").ToString());
    }

    [Fact]
    public void CanDetectSwitchedRussianWords()
    {
        var tokenizer = RekeyTokenizer.Create();

        Assert.Equal("частица", tokenizer.Tokenize("xfcnbwf").ToString());
        Assert.Equal("адрес", tokenizer.Tokenize("flhtc").ToString());
        Assert.Equal("пирамида", tokenizer.Tokenize("gbhfvblf").ToString());
    }

    [Fact]
    public void CanDetectSwitchedRussianWordsWithSeparators()
    {
        var tokenizer = RekeyTokenizer.Create();

        Assert.Equal("бильярд", tokenizer.Tokenize(",bkmzhl").ToString());
        Assert.Equal("любовь", tokenizer.Tokenize("k.,jdm").ToString());
        Assert.Equal("подъезд", tokenizer.Tokenize("gjl]tpl").ToString());
    }

    // === Ukrainian tests ===

    [Fact]
    public void CanDetectCorrectUkrainianWords()
    {
        var tokenizer = RekeyTokenizer.Create();

        Assert.Equal("привіт", tokenizer.Tokenize("привіт").ToString());
        Assert.Equal("україна", tokenizer.Tokenize("україна").ToString());
        Assert.Equal("людина", tokenizer.Tokenize("людина").ToString());
        Assert.Equal("сонце", tokenizer.Tokenize("сонце").ToString());
    }

    [Fact]
    public void CanDetectSwitchedUkrainianWords()
    {
        // Ukrainian words typed in English layout
        var tokenizer = RekeyTokenizer.Create();

        // "привіт" (hello) typed in English mode: ghbdsn
        Assert.Equal("привіт", tokenizer.Tokenize("ghbdsn").ToString());

        // "людина" (person) typed in English mode: k.lbyf
        Assert.Equal("людина", tokenizer.Tokenize("k.lbyf").ToString());

        // "сонце" (sun) typed in English mode: cjywt
        Assert.Equal("сонце", tokenizer.Tokenize("cjywt").ToString());

        // "місто" (city) typed in English mode: vscnj
        Assert.Equal("місто", tokenizer.Tokenize("vscnj").ToString());
    }

    [Fact]
    public void CanDetectSwitchedUkrainianWordsWithUniqueChars()
    {
        var tokenizer = RekeyTokenizer.Create();

        // "їжак" (hedgehog) typed in English: ];fr
        Assert.Equal("їжак", tokenizer.Tokenize("];fr").ToString());

        // "єдність" (unity) typed in English: 'lyscnm
        Assert.Equal("єдність", tokenizer.Tokenize("'lyscnm").ToString());
    }

    [Fact]
    public void CanDetectEnglishTypedInUkrainianLayout()
    {
        var tokenizer = RekeyTokenizer.Create();

        // "hello" typed in Ukrainian mode: рудщщ (р=h, у=e, д=l, щ=o)
        Assert.Equal("hello", tokenizer.Tokenize("руддщ").ToString());

        // "world" typed in Ukrainian mode: цщкдв
        Assert.Equal("world", tokenizer.Tokenize("цщкдв").ToString());

        // "music" typed in Ukrainian mode: ьгішс (і instead of ы!)
        Assert.Equal("music", tokenizer.Tokenize("ьгішс").ToString());
    }

    // === General tests ===

    [Fact]
    public void CanDetectDigits()
    {
        var tokenizer = RekeyTokenizer.Create();

        Assert.Equal("24", tokenizer.Tokenize("24").ToString());
        Assert.Equal("борьба24", tokenizer.Tokenize(",jhm,f24").ToString());
        Assert.Equal("4test", tokenizer.Tokenize("4еуые").ToString());
        Assert.Equal("life4good", tokenizer.Tokenize("дшау4пщщв").ToString());
    }

    [Fact]
    public void CanDetectUppercaseLetters()
    {
        var tokenizer = RekeyTokenizer.Create();

        Assert.Equal("БЮРО", tokenizer.Tokenize("<>HJ").ToString());
        Assert.Equal("Бюро", tokenizer.Tokenize("<.hj").ToString());
        Assert.Equal("Ход", tokenizer.Tokenize("{jl").ToString());

        Assert.Equal("MAY tHe fORce BE with yoU", tokenizer.Tokenize("ЬФН еРу аЩКсу ИУ цшер нщГ").ToString());
    }

    [Fact]
    public void LeaveAsIsIfUnknown()
    {
        var tokenizer = RekeyTokenizer.Create();

        Assert.Equal("xcvn", tokenizer.Tokenize("xcvn").ToString());
        Assert.Equal("чсмт", tokenizer.Tokenize("чсмт").ToString());
        Assert.Equal("вбрз", tokenizer.Tokenize("вбрз").ToString());
    }

    [Fact]
    public void LeaveAsIsIfEnAbbreviations()
    {
        var tokenizer = RekeyTokenizer.Create();

        Assert.Equal("T.G.I", tokenizer.Tokenize("T.G.I").ToString());
        Assert.Equal("r.i.p.", tokenizer.Tokenize("r.i.p.").ToString());
    }

    [Fact]
    public void CorrectedAppearsOnlyOnLangSwitch()
    {
        var tokenizer = RekeyTokenizer.Create();

        Assert.NotNull(tokenizer.Tokenize("<>HJ").Corrected);
        Assert.NotNull(tokenizer.Tokenize("ьгыШс").Corrected);
        Assert.Null(tokenizer.Tokenize("tEst").Corrected);
        Assert.Null(tokenizer.Tokenize("Слово").Corrected);
        Assert.Null(tokenizer.Tokenize("Привіт").Corrected);
    }
}
