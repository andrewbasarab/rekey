using System.Reflection;

namespace RekeyNet;

/// <summary>
/// Detects text typed in the wrong keyboard layout (EN↔RU, EN↔UK) and corrects it.
/// Works like Punto Switcher using n-gram analysis.
/// Supports English, Russian, and Ukrainian.
/// </summary>
/// <example>
/// <code>
/// var rekey = new Rekey();
/// string fixed = rekey.Correct("ghbdsn");   // "привіт"
/// </code>
/// </example>
public sealed class Rekey
{
    private const char Apostrophe = '\'';
    private const char Apostrophe1 = '`';

    // Confidence tiers (see RekeyResult.Confidence)
    private const double ConfidenceException = 0.95;
    private const double ConfidenceKnownWord = 0.9;
    private const double ConfidenceSwitch = 0.8;
    private const double ConfidenceAmbiguous = 0.55;

    private static readonly Lazy<Rekey> Shared = new(() => new Rekey());

    private static readonly Lang[] AllCyrillicLangs = [Lang.Ru, Lang.Uk, Lang.Be];

    private readonly NgramLangChecker _langChecker;
    private readonly Dictionary<string, string> _exceptions;
    private readonly Dictionary<Lang, HashSet<string>> _knownWords;
    private readonly bool _enEnabled;
    private readonly Lang[] _cyrillicLangs; // enabled Cyrillic languages, in priority order
    private readonly int _minWordLength;
    private readonly bool _smartFiltering;

    /// <summary>
    /// Creates a new instance with default settings. Loads the embedded n-gram
    /// dictionaries once, so prefer reusing a single instance (it is stateless and
    /// thread-safe). For dependency injection, register as a singleton.
    /// </summary>
    public Rekey() : this(new RekeyOptions())
    {
    }

    /// <summary>
    /// Creates a new instance that leaves words shorter than
    /// <paramref name="minWordLength"/> untouched (they are too short to detect a
    /// layout reliably).
    /// </summary>
    public Rekey(int minWordLength) : this(new RekeyOptions { MinWordLength = minWordLength })
    {
    }

    /// <summary>
    /// Creates a new instance with the given <paramref name="options"/> — e.g. to
    /// disable a language or change language priority.
    /// </summary>
    public Rekey(RekeyOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));
        var languages = options.Languages.Distinct().ToArray();
        if (languages.Length == 0)
            throw new ArgumentException("At least one language must be enabled.", nameof(options));

        _enEnabled = languages.Contains(Lang.En);
        _cyrillicLangs = languages.Where(l => l is Lang.Ru or Lang.Uk or Lang.Be).ToArray();
        _minWordLength = options.MinWordLength;
        _smartFiltering = options.SmartFiltering;
        _langChecker = NgramLangChecker.Create(languages);
        _exceptions = LoadExceptions();

        // The known-word tie-break is only needed when Cyrillic languages compete.
        _knownWords = [];
        if (_cyrillicLangs.Length > 1)
        {
            foreach (var lang in _cyrillicLangs)
                _knownWords[lang] = LoadWordSet($"knownwords-{NgramLangChecker.FileSuffix(lang)}.txt");
        }
    }

    /// <summary>
    /// A shared, lazily-created instance with default settings — convenient when you
    /// don't use dependency injection.
    /// </summary>
    public static Rekey Default => Shared.Value;

    /// <summary>
    /// Returns the text with the keyboard layout corrected, or the original text
    /// unchanged if no correction was needed.
    /// </summary>
    public string Correct(string text) => Analyze(text).Text;

    /// <summary>
    /// Analyzes the text and returns a detailed result (original, corrected, words,
    /// and whether a correction was applied).
    /// </summary>
    public RekeyResult Analyze(string input)
    {
        if (!_smartFiltering || string.IsNullOrEmpty(input))
            return AnalyzeCore(input);

        var segments = SplitByWhitespace(input);
        if (!segments.Any(s => !char.IsWhiteSpace(s[0]) && IsProtectedToken(s)))
            return AnalyzeCore(input);

        var corrected = new System.Text.StringBuilder(input.Length);
        var words = new List<string>();
        double confidence = 1.0;

        foreach (var segment in segments)
        {
            if (char.IsWhiteSpace(segment[0]) || IsProtectedToken(segment))
            {
                corrected.Append(segment);
                continue;
            }

            var result = AnalyzeCore(segment);
            corrected.Append(result.Text);
            words.AddRange(result.Words);
            if (result.Confidence < confidence)
                confidence = result.Confidence;
        }

        string correctedText = corrected.ToString();
        string? correctedResult = Canonical(correctedText) == Canonical(input) ? null : correctedText;
        return new RekeyResult(input, correctedResult, words, correctedResult is null ? 1.0 : confidence);
    }

    private RekeyResult AnalyzeCore(string input)
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

        double confidence = 1.0;
        if (correctedResult != null)
        {
            foreach (var token in allTokens)
            {
                if (token.Confidence < confidence)
                    confidence = token.Confidence;
            }
        }

        return new RekeyResult(input, correctedResult, wordTokens, confidence);
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
    /// Try the enabled Cyrillic languages in priority order.
    /// </summary>
    private List<Token> EnOrPossibleCyrillic(Token token)
    {
        string corrected = token.Canonical;
        double confidence = 1.0;

        if (!(_enEnabled && _langChecker.Check(Lang.En, token.Canonical))
            && SwitchToCyrillic(token.Canonical) is { } switched)
        {
            corrected = switched.Word;
            confidence = switched.Confidence;
        }

        return [BuildToken(token.Type, token.Corrected, token.Canonical, corrected, confidence)];
    }

    /// <summary>
    /// Switches a Latin token into each enabled Cyrillic language and returns the most
    /// plausible result, or null when none is plausible. The RU and UK layouts differ
    /// only on the s/]/'/` keys (ы/ъ/э/ё vs і/ї/є/ґ), so a wrong-layout token often
    /// switches to an n-gram-plausible word in BOTH languages (e.g. "ghbdsn" →
    /// "привыт"/"привіт"). N-grams cannot arbitrate that; the embedded known-word lists
    /// can. Falls back to configured language priority when neither word is known.
    /// </summary>
    private (string Word, double Confidence)? SwitchToCyrillic(string canonical)
    {
        // Collect the plausible candidates per enabled Cyrillic language, in priority order.
        var candidates = new List<(Lang Lang, string Word)>(_cyrillicLangs.Length);
        bool allSame = true;

        foreach (var lang in _cyrillicLangs)
        {
            string switched = Characters.SwitchLang(canonical, lang);
            if (!_langChecker.Check(lang, switched))
                continue;
            if (candidates.Count > 0 && candidates[0].Word != switched)
                allSame = false;
            candidates.Add((lang, switched));
        }

        if (candidates.Count == 0)
            return null;
        if (candidates.Count == 1 || allSame)
            return (candidates[0].Word, ConfidenceSwitch);

        // Several different plausible words — prefer a known dictionary word.
        foreach (var (lang, word) in candidates)
        {
            if (_knownWords.TryGetValue(lang, out var known) && known.Contains(word))
                return (word, ConfidenceKnownWord);
        }

        return (candidates[0].Word, ConfidenceAmbiguous);
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
            if (SwitchToCyrillic(token.Canonical) is { } switched)
                return [BuildToken(token.Type, token.Corrected, token.Canonical, switched.Word, switched.Confidence)];

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

        // Languages whose alphabet covers every character of the token — the only
        // languages it could have been typed in. (E.g. "ы" rules out Ukrainian, "ї"
        // rules out Russian and Belarusian.)
        var fits = new List<Lang>(_cyrillicLangs.Length);
        foreach (var lang in _cyrillicLangs)
        {
            if (Characters.WordFitsLang(lang, token.Canonical))
                fits.Add(lang);
        }

        bool valid = false;
        foreach (var lang in fits)
        {
            if (_langChecker.Check(lang, token.Canonical))
            {
                valid = true;
                break;
            }
        }

        if (!valid && _enEnabled)
        {
            // Not a plausible word in any fitting language — try reading it as English
            // typed on one of the Cyrillic layouts.
            foreach (var lang in fits.Count > 0 ? (IReadOnlyList<Lang>)fits : SwitchSourceLangs())
            {
                string switched = Characters.SwitchToEn(token.Canonical, lang);
                if (_langChecker.Check(Lang.En, switched))
                {
                    corrected = switched;
                    break;
                }
            }
        }

        double confidence = corrected == token.Canonical ? 1.0 : ConfidenceSwitch;
        return [BuildToken(token.Type, token.Corrected, token.Canonical, corrected, confidence)];
    }

    /// <summary>
    /// Cyrillic layouts to try when converting a Cyrillic token back to English.
    /// With no Cyrillic language enabled (EN-only config), still try both physical
    /// layouts — the token had to be typed on one of them.
    /// </summary>
    private Lang[] SwitchSourceLangs() => _cyrillicLangs.Length > 0 ? _cyrillicLangs : AllCyrillicLangs;

    /// <summary>
    /// Token has Cyrillic chars that could be separators (б,ю,ж,х,ъ,ї,ґ etc.).
    /// </summary>
    private List<Token> CyrillicOrPossibleSeparator(Token token)
    {
        // Valid in any enabled Cyrillic language → keep as typed
        foreach (var lang in _cyrillicLangs)
        {
            if (_langChecker.Check(lang, token.Canonical))
                return [BuildToken(token.Type, token.Corrected, token.Canonical, token.Canonical)];
        }

        if (_enEnabled)
        {
            // Try reading the token as English typed on one of the Cyrillic layouts
            foreach (var lang in SwitchSourceLangs())
            {
                var split = SplitBySpecificSeparators(
                    Characters.SwitchToEn(token.Canonical, lang),
                    Characters.IsSeparatorOrPossibleRu,
                    useExceptions: true);
                if (CheckAll(split, Lang.En))
                {
                    // Tokens were built from the already-switched text; mark the words
                    // as corrections so confidence propagates.
                    return split
                        .Select(t => t.IsWord
                            ? new Token(t.Type, t.Original, t.Canonical, t.Corrected, t.CharTypes, ConfidenceSwitch)
                            : t)
                        .ToList();
                }
            }
        }

        return [BuildToken(token.Type, token.Corrected, token.Canonical, token.Canonical)];
    }

    /// <summary>Splits into alternating runs of whitespace and non-whitespace (none empty).</summary>
    private static List<string> SplitByWhitespace(string input)
    {
        var segments = new List<string>();
        int start = 0;
        for (int i = 1; i <= input.Length; i++)
        {
            if (i == input.Length || char.IsWhiteSpace(input[i]) != char.IsWhiteSpace(input[start]))
            {
                segments.Add(input[start..i]);
                start = i;
            }
        }
        return segments;
    }

    /// <summary>
    /// True for tokens that look intentional rather than mistyped — URLs, e-mails,
    /// camelCase identifiers, mixed Latin+Cyrillic — which must never be "corrected".
    /// </summary>
    private static bool IsProtectedToken(string token)
    {
        if (token.IndexOf("://", StringComparison.Ordinal) >= 0
            || token.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            return true;

        int at = token.IndexOf('@');
        if (at > 0 && at < token.Length - 1 && at == token.LastIndexOf('@')
            && token.IndexOf('.', at) > at + 1)
            return true;

        bool hasLatin = false, hasCyrillic = false, camelCase = false;
        for (int i = 0; i < token.Length; i++)
        {
            char c = token[i];
            if (c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z'))
            {
                hasLatin = true;
                // internal lowercase→uppercase transition between Latin letters
                if (i > 0 && c is >= 'A' and <= 'Z' && token[i - 1] is >= 'a' and <= 'z')
                    camelCase = true;
            }
            else
            {
                char lower = char.ToLowerInvariant(c);
                if (Characters.IsRussianChar(lower) || Characters.IsUkrainianChar(lower))
                    hasCyrillic = true;
            }
        }

        if (hasLatin && hasCyrillic) return true;      // mixed script — likely a brand name
        if (camelCase && !hasCyrillic) return true;    // code-like identifier
        return false;
    }

    private static string Canonical(string candidate) =>
        candidate.Replace(Apostrophe1, Apostrophe).ToLowerInvariant();

    private Token BuildToken(TokenType tokenType, string original, string canonical, string corrected,
        double confidence = 1.0) =>
        BuildToken(tokenType, original, canonical, corrected, default, useException: true, confidence);

    private Token BuildToken(TokenType tokenType, string original, string canonical, string corrected,
        CharTypeSet charTypes, bool useException = true, double confidence = 1.0)
    {
        string? exception = null;
        bool isException = useException && _exceptions.TryGetValue(canonical, out exception);

        var type = isException ? TokenType.Word : tokenType;

        var correctedValue = isException
            ? exception!
            : canonical.Length < _minWordLength
                ? canonical
                : corrected;

        double confidenceValue = correctedValue == canonical
            ? 1.0
            : isException
                ? ConfidenceException
                : confidence;

        return new Token(type, original, canonical, correctedValue, charTypes, confidenceValue);
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

    private static HashSet<string> LoadWordSet(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"RekeyNet.Resources.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {resourceName}");

        using var reader = new StreamReader(stream);
        var words = new HashSet<string>();
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (!string.IsNullOrEmpty(line))
                words.Add(line);
        }
        return words;
    }

    private static Dictionary<string, string> LoadExceptions()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "RekeyNet.Resources.exceptions.csv";

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
