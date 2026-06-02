using System.Reflection;

namespace Rekey;

/// <summary>
/// A tokenizer that detects wrong keyboard layout (EN↔RU, EN↔UK) and corrects it.
/// Works like Punto Switcher using n-gram analysis.
/// Supports English, Russian, and Ukrainian.
/// </summary>
public sealed class RekeyTokenizer : ITokenizer
{
    private const char Apostrophe = '\'';
    private const char Apostrophe1 = '`';

    private readonly NgramLangChecker _langChecker;
    private readonly Dictionary<string, string> _exceptions;
    private readonly int _minTokenLength;

    internal RekeyTokenizer(NgramLangChecker langChecker, int minTokenLength)
    {
        _langChecker = langChecker;
        _minTokenLength = minTokenLength;
        _exceptions = LoadExceptions();
    }

    /// <summary>Creates a new tokenizer instance with default settings.</summary>
    public static RekeyTokenizer Create() =>
        new(NgramLangChecker.Create(), 0);

    /// <summary>Creates a new tokenizer instance with minimum token length.</summary>
    public static RekeyTokenizer Create(int minTokenLength) =>
        new(NgramLangChecker.Create(), minTokenLength);

    public TokenizerResponse Tokenize(string input)
    {
        var uppercasePositions = Characters.UppercasePositions(input);
        string canonical = Canonical(input);
        var allTokens = Split(canonical);

        var wordTokens = allTokens
            .Where(t => t.IsWord)
            .Select(t => t.Corrected)
            .ToList();

        string correctedRaw = string.Concat(allTokens.Select(t => t.Corrected));
        string corrected = Characters.RestoreUppercase(correctedRaw, uppercasePositions);

        string? correctedResult = Canonical(corrected) == canonical
            ? null
            : corrected;

        return new TokenizerResponse(input, correctedResult, wordTokens);
    }

    internal List<Token> Split(string input)
    {
        var tokens = new List<Token>();
        foreach (var token in SplitBySpecificSeparators(input, Characters.IsSeparator, useExceptions: false))
        {
            tokens.AddRange(SplitPossibleSubTokens(token));
        }
        return tokens;
    }

    internal List<Token> SplitBySpecificSeparators(string original, Func<char, bool> isSeparator, bool useExceptions)
    {
        if (string.IsNullOrEmpty(original))
            return [];

        var tokens = new List<Token>();
        bool isPrevSeparator = false;
        bool isPrevDigit = false;
        int start = 0;
        var charTypes = new CharTypeSet();

        for (int i = 0; i < original.Length; i++)
        {
            char ch = original[i];
            bool isCurrentSeparator = isSeparator(ch);
            bool isCurrentDigit = char.IsDigit(ch);

            if (i > 0 && (isCurrentSeparator ^ isPrevSeparator || isCurrentDigit ^ isPrevDigit))
            {
                string tokenStr = original[start..i];
                tokens.Add(BuildToken(TokenTypeExtensions.Of(isPrevSeparator), tokenStr, tokenStr, tokenStr, charTypes, useExceptions));
                start = i;
                charTypes = new CharTypeSet();
            }

            isPrevSeparator = isCurrentSeparator;
            isPrevDigit = isCurrentDigit;
            charTypes.Add(CharTypeHelper.Of(char.ToLower(ch)));
        }

        string lastToken = original[start..];
        tokens.Add(BuildToken(TokenTypeExtensions.Of(isPrevSeparator), lastToken, lastToken, lastToken, charTypes, useExceptions));

        return tokens;
    }

    private List<Token> SplitPossibleSubTokens(Token token)
    {
        var charTypes = token.CharTypes;

        if (charTypes.ContainsOnly(CharType.EnOrPossibleRu))
            return EnOrPossibleCyrillic(token);

        if (charTypes.ContainsOnlyFirstOrBoth(CharType.SeparatorOrPossibleRu, CharType.EnOrPossibleRu))
            return SeparatorOrPossibleEn(token);

        if (charTypes.ContainsOnly(CharType.RuOrPossibleEn))
            return CyrillicOrPossibleEn(token);

        if (charTypes.ContainsOnlyFirstOrBoth(CharType.RuOrPossibleSeparator, CharType.RuOrPossibleEn))
            return CyrillicOrPossibleSeparator(token);

        return [token];
    }

    /// <summary>
    /// Token looks like English but might be Cyrillic typed in wrong layout.
    /// Try switching to RU first (higher traffic), then UK.
    /// </summary>
    private List<Token> EnOrPossibleCyrillic(Token token)
    {
        string corrected = token.Canonical;

        if (!_langChecker.Check(Lang.En, token.Canonical))
        {
            // Try Russian first (higher traffic), then Ukrainian
            string switchedRu = Characters.SwitchLang(token.Canonical, Lang.Ru);
            string switchedUk = Characters.SwitchLang(token.Canonical, Lang.Uk);

            if (_langChecker.Check(Lang.Ru, switchedRu))
                corrected = switchedRu;
            else if (_langChecker.Check(Lang.Uk, switchedUk))
                corrected = switchedUk;
        }

        return [BuildToken(token.Type, token.Corrected, token.Canonical, corrected)];
    }

    /// <summary>
    /// Token has separator chars mixed with English chars - might be Cyrillic.
    /// For example: ",bkmzhl" → "бильярд" (RU) or similar patterns for UK.
    /// </summary>
    private List<Token> SeparatorOrPossibleEn(Token token)
    {
        if (Characters.IsAbbreviation(token.Canonical))
        {
            return SplitBySpecificSeparators(token.Canonical, Characters.IsSeparatorOrPossibleRu, useExceptions: false);
        }
        else
        {
            // Try Russian first (higher traffic), then Ukrainian
            string switchedRu = Characters.SwitchLang(token.Canonical, Lang.Ru);
            if (_langChecker.Check(Lang.Ru, switchedRu))
                return [BuildToken(token.Type, token.Corrected, token.Canonical, switchedRu)];

            string switchedUk = Characters.SwitchLang(token.Canonical, Lang.Uk);
            if (_langChecker.Check(Lang.Uk, switchedUk))
                return [BuildToken(token.Type, token.Corrected, token.Canonical, switchedUk)];

            return SplitBySpecificSeparators(token.Canonical, Characters.IsSeparatorOrPossibleRu, useExceptions: true);
        }
    }

    /// <summary>
    /// Token looks like Cyrillic but might be English typed in wrong layout.
    /// Detect if it's Russian or Ukrainian, then try switching to English.
    /// </summary>
    private List<Token> CyrillicOrPossibleEn(Token token)
    {
        string corrected = token.Canonical;

        // Determine which Cyrillic language this could be
        bool hasUkChars = Characters.HasUkrainianSpecificChars(token.Canonical);
        bool hasRuChars = Characters.HasRussianSpecificChars(token.Canonical);

        if (hasUkChars && !hasRuChars)
        {
            // Definitely Ukrainian chars → check UK, then try EN
            if (!_langChecker.Check(Lang.Uk, token.Canonical))
            {
                string switched = Characters.SwitchToEn(token.Canonical, Lang.Uk);
                if (_langChecker.Check(Lang.En, switched))
                    corrected = switched;
            }
        }
        else if (hasRuChars && !hasUkChars)
        {
            // Definitely Russian chars → check RU, then try EN
            if (!_langChecker.Check(Lang.Ru, token.Canonical))
            {
                string switched = Characters.SwitchToEn(token.Canonical, Lang.Ru);
                if (_langChecker.Check(Lang.En, switched))
                    corrected = switched;
            }
        }
        else
        {
            // Ambiguous (shared chars) → try both, RU has priority
            if (!_langChecker.Check(Lang.Ru, token.Canonical)
                && !_langChecker.Check(Lang.Uk, token.Canonical))
            {
                // Try switching from RU layout first
                string switchedFromRu = Characters.SwitchToEn(token.Canonical, Lang.Ru);
                if (_langChecker.Check(Lang.En, switchedFromRu))
                {
                    corrected = switchedFromRu;
                }
                else
                {
                    // Try switching from UK layout
                    string switchedFromUk = Characters.SwitchToEn(token.Canonical, Lang.Uk);
                    if (_langChecker.Check(Lang.En, switchedFromUk))
                        corrected = switchedFromUk;
                }
            }
        }

        return [BuildToken(token.Type, token.Corrected, token.Canonical, corrected)];
    }

    /// <summary>
    /// Token has Cyrillic chars that could be separators (б,ю,ж,х,ъ,ї,ґ etc.).
    /// </summary>
    private List<Token> CyrillicOrPossibleSeparator(Token token)
    {
        // Check if valid in Russian or Ukrainian (RU has priority)
        bool correctRu = _langChecker.Check(Lang.Ru, token.Canonical);
        bool correctUk = !correctRu && _langChecker.Check(Lang.Uk, token.Canonical);
        bool correct = correctRu || correctUk;

        List<Token> splitByPossibleSeparators = [];

        if (!correct)
        {
            // Try switching to EN from RU layout first, then UK
            splitByPossibleSeparators = SplitBySpecificSeparators(
                Characters.SwitchToEn(token.Canonical, Lang.Ru),
                Characters.IsSeparatorOrPossibleRu,
                useExceptions: true);
            correct = !CheckAll(splitByPossibleSeparators, Lang.En);

            if (correct)
            {
                // Also try from UK layout
                var splitUk = SplitBySpecificSeparators(
                    Characters.SwitchToEn(token.Canonical, Lang.Uk),
                    Characters.IsSeparatorOrPossibleRu,
                    useExceptions: true);
                if (!CheckAll(splitUk, Lang.En))
                    correct = true;
                else
                    correct = false;
            }
        }

        if (correct)
        {
            return [BuildToken(token.Type, token.Corrected, token.Canonical, token.Canonical)];
        }
        else
        {
            return splitByPossibleSeparators;
        }
    }

    private static string Canonical(string candidate) =>
        candidate.Replace(Apostrophe1, Apostrophe).ToLowerInvariant();

    private Token BuildToken(TokenType tokenType, string original, string canonical, string corrected) =>
        BuildToken(tokenType, original, canonical, corrected, default, useException: true);

    private Token BuildToken(TokenType tokenType, string original, string canonical, string corrected,
        CharTypeSet charTypes, bool useException = true)
    {
        var type = useException && _exceptions.ContainsKey(canonical)
            ? TokenType.Word
            : tokenType;

        var correctedValue = useException && _exceptions.TryGetValue(canonical, out string? exception)
            ? exception
            : canonical.Length < _minTokenLength
                ? canonical
                : corrected;

        return new Token(type, original, canonical, correctedValue, charTypes);
    }

    private bool CheckAll(IEnumerable<Token> tokens, Lang lang)
    {
        bool atLeastOneWord = false;
        foreach (var token in tokens)
        {
            atLeastOneWord = token.IsWord;
            if (token.IsWord && !_langChecker.Check(lang, token.Corrected))
                return false;
        }
        return atLeastOneWord;
    }

    private static Dictionary<string, string> LoadExceptions()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Rekey.Resources.exceptions.csv";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {resourceName}");

        using var reader = new StreamReader(stream);
        var exceptions = new Dictionary<string, string>();
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split('|');
            if (parts.Length == 2)
                exceptions[parts[0].Trim()] = parts[1].Trim();
        }
        return exceptions;
    }
}
