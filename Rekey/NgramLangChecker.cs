using System.Reflection;

namespace RekeyNet;

internal sealed class NgramLangChecker
{
    private readonly Dictionary<Lang, HashSet<string>> _nonexistent2gram;
    private readonly Dictionary<Lang, HashSet<string>> _nonexistent3gram;
    private readonly Dictionary<Lang, HashSet<string>> _nonexistentFirst4gram;
    private readonly Dictionary<Lang, HashSet<string>> _nonexistent4gram;

    public NgramLangChecker(
        Dictionary<Lang, HashSet<string>> nonexistent2gram,
        Dictionary<Lang, HashSet<string>> nonexistent3gram,
        Dictionary<Lang, HashSet<string>> nonexistentFirst4gram,
        Dictionary<Lang, HashSet<string>> nonexistent4gram)
    {
        _nonexistent2gram = nonexistent2gram;
        _nonexistent3gram = nonexistent3gram;
        _nonexistentFirst4gram = nonexistentFirst4gram;
        _nonexistent4gram = nonexistent4gram;
    }

    public static NgramLangChecker Create()
    {
        return new NgramLangChecker(
            new Dictionary<Lang, HashSet<string>>
            {
                [Lang.En] = ReadVocabulary("nonexistent2gram-en.txt"),
                [Lang.Ru] = ReadVocabulary("nonexistent2gram-ru.txt"),
                [Lang.Uk] = ReadVocabulary("nonexistent2gram-uk.txt")
            },
            new Dictionary<Lang, HashSet<string>>
            {
                [Lang.En] = ReadVocabulary("nonexistent3gram-en.txt"),
                [Lang.Ru] = ReadVocabulary("nonexistent3gram-ru.txt"),
                [Lang.Uk] = ReadVocabulary("nonexistent3gram-uk.txt")
            },
            new Dictionary<Lang, HashSet<string>>
            {
                [Lang.En] = ReadVocabulary("nonexistentFirst4gram-en.txt"),
                [Lang.Ru] = ReadVocabulary("nonexistentFirst4gram-ru.txt"),
                [Lang.Uk] = ReadVocabulary("nonexistentFirst4gram-uk.txt")
            },
            new Dictionary<Lang, HashSet<string>>
            {
                [Lang.En] = ReadVocabulary("nonexistent4gram-en.txt"),
                [Lang.Ru] = ReadVocabulary("nonexistent4gram-ru.txt"),
                [Lang.Uk] = ReadVocabulary("nonexistent4gram-uk.txt")
            });
    }

    public bool Check(Lang lang, string word)
    {
        int length = word.Length;

        if (!Characters.HasVowel(lang, word))
            return false;

        if (length >= 6)
        {
            string? firstConsonant6gram = FirstNgram(lang, word, 6, vowel: false);
            if (firstConsonant6gram != null)
                return false;
        }

        if (length >= 4)
        {
            if (_nonexistentFirst4gram[lang].Contains(word[..4]))
                return false;
        }

        if (length >= 3)
        {
            string? firstVowel3gram = FirstNgram(lang, word, 3, vowel: true);
            if (firstVowel3gram != null && _nonexistent3gram[lang].Contains(firstVowel3gram))
                return false;
        }

        if (length >= 4)
        {
            string? firstConsonant4gram = FirstNgram(lang, word, 4, vowel: false);
            if (firstConsonant4gram != null && _nonexistent4gram[lang].Contains(firstConsonant4gram))
                return false;
        }

        var nonexistent2grams = _nonexistent2gram[lang];
        if (length >= 2)
        {
            if (nonexistent2grams.Contains("*" + word[..2])
                || nonexistent2grams.Contains(word[(length - 2)..] + "*"))
                return false;
        }

        if (length >= 4)
        {
            for (int i = 1; i < length - 2; i++)
            {
                if (nonexistent2grams.Contains(word[i..(i + 2)]))
                    return false;
            }
        }

        return true;
    }

    internal static string? FirstNgram(Lang lang, string word, int n, bool vowel)
    {
        for (int begin = 0, end = n; end <= word.Length; begin++, end++)
        {
            bool check = true;
            for (int i = begin; i < end; i++)
            {
                if (vowel && !Characters.IsVowel(lang, word[i])
                    || !vowel && !Characters.IsConsonant(lang, word[i]))
                {
                    check = false;
                    break;
                }
            }
            if (check)
                return word[begin..end];
        }

        return null;
    }

    private static HashSet<string> ReadVocabulary(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"RekeyNet.Resources.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {resourceName}");

        using var reader = new StreamReader(stream);
        var lines = new HashSet<string>();
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (!string.IsNullOrEmpty(line))
                lines.Add(line);
        }
        return lines;
    }
}
