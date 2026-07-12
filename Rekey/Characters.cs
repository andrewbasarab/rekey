namespace RekeyNet;

internal static class Characters
{
    private static readonly char[] VowelsRu =
        ['а', 'е', 'и', 'о', 'у', 'ы', 'э', 'ю', 'я', 'ё'];

    private static readonly char[] ConsonantsRu =
        ['б', 'в', 'г', 'д', 'ж', 'з', 'й', 'к', 'л', 'м', 'н',
         'п', 'р', 'с', 'т', 'ф', 'х', 'ц', 'ч', 'ш', 'щ', 'ъ', 'ь'];

    private static readonly char[] VowelsUk =
        ['а', 'е', 'є', 'и', 'і', 'ї', 'о', 'у', 'ю', 'я'];

    private static readonly char[] ConsonantsUk =
        ['б', 'в', 'г', 'ґ', 'д', 'ж', 'з', 'й', 'к', 'л', 'м', 'н',
         'п', 'р', 'с', 'т', 'ф', 'х', 'ц', 'ч', 'ш', 'щ', 'ь'];

    private static readonly char[] VowelsBe =
        ['а', 'е', 'ё', 'і', 'о', 'у', 'ы', 'э', 'ю', 'я'];

    private static readonly char[] ConsonantsBe =
        ['б', 'в', 'г', 'д', 'ж', 'з', 'й', 'к', 'л', 'м', 'н',
         'п', 'р', 'с', 'т', 'ў', 'ф', 'х', 'ц', 'ч', 'ш', 'ь'];

    private static readonly char[] VowelsEn =
        ['a', 'e', 'i', 'o', 'u', 'y'];

    private static readonly char[] ConsonantsEn =
        ['b', 'c', 'd', 'f', 'h', 'g', 'j', 'k', 'l', 'm',
         'n', 'p', 'q', 'r', 's', 't', 'v', 'w', 'x', 'z'];

    private static readonly Dictionary<Lang, char[]> Vowels = new()
    {
        [Lang.Ru] = VowelsRu,
        [Lang.En] = VowelsEn,
        [Lang.Uk] = VowelsUk,
        [Lang.Be] = VowelsBe
    };

    private static readonly Dictionary<Lang, char[]> Consonants = new()
    {
        [Lang.Ru] = ConsonantsRu,
        [Lang.En] = ConsonantsEn,
        [Lang.Uk] = ConsonantsUk,
        [Lang.Be] = ConsonantsBe
    };

    private static readonly HashSet<char> SeparatorsSet = new(
        [' ', '\t', '\r', '\n', '!', '?', '_', '=', '-', '+', '*', '/', '|', '\\', '@', '#', '$', '%', '^', '&', '(', ')']);

    private static readonly HashSet<char> SeparatorOrPossibleRuSet = new(
        ['<', ',', '~', '`', ':', ';', '{', '[', '}', ']', '"', '\'', '>', '.']);

    private static readonly HashSet<char> EnOrPossibleRuSet = new(
        ['f', 'd', 'u', 'l', 't', 'p', 'b', 'q', 'r', 'k', 'v', 'y', 'j', 'g', 'h', 'c', 'n', 'e', 'a',
         'w', 'x', 'i', 'o', 's', 'm', '\'', 'z']);

    // Cyrillic chars that could be English. Includes RU-specific (ы,э), UK-specific (і,є)
    // and BE-specific (ў) letters.
    private static readonly HashSet<char> RuOrPossibleEnSet = new(
        ['ф', 'и', 'с', 'в', 'у', 'а', 'п', 'р', 'ш', 'о', 'л', 'д', 'ь', 'т', 'щ',
         'з', 'й', 'к', 'ы', 'е', 'г', 'м', 'ц', 'ч', 'н', 'я', 'э',
         'і', 'є', 'ў']);

    // Cyrillic chars that could be separators. Includes RU-specific (ё,ъ) and UK-specific (ї,ґ)
    private static readonly HashSet<char> RuOrPossibleSeparatorSet = new(
        ['б', 'ё', 'ж', 'х', 'ъ', 'э', 'ю',
         'ї', 'ґ', 'є']);

    private static readonly HashSet<char> PossibleRuInUppercaseSet = new(
        ['<', '~', ':', '{', '}', '"', '>']);

    // EN → RU
    private static readonly Dictionary<char, char> SwitchRuFromEn = new()
    {
        ['f'] = 'а', [','] = 'б', ['<'] = 'б', ['d'] = 'в', ['u'] = 'г',
        ['l'] = 'д', ['t'] = 'е', ['`'] = 'ё', ['~'] = 'ё',
        [';'] = 'ж', [':'] = 'ж', ['p'] = 'з', ['b'] = 'и',
        ['q'] = 'й', ['r'] = 'к', ['k'] = 'л', ['v'] = 'м',
        ['y'] = 'н', ['j'] = 'о', ['g'] = 'п', ['h'] = 'р',
        ['c'] = 'с', ['n'] = 'т', ['e'] = 'у', ['a'] = 'ф',
        ['['] = 'х', ['{'] = 'х', ['w'] = 'ц', ['x'] = 'ч',
        ['i'] = 'ш', ['o'] = 'щ', [']'] = 'ъ', ['}'] = 'ъ',
        ['s'] = 'ы', ['m'] = 'ь',
        ['\''] = 'э', ['"'] = 'э', ['.'] = 'ю', ['>'] = 'ю', ['z'] = 'я'
    };

    // RU → EN
    private static readonly Dictionary<char, char> SwitchEnFromRu = new()
    {
        ['ф'] = 'a', ['и'] = 'b', ['с'] = 'c', ['в'] = 'd',
        ['у'] = 'e', ['а'] = 'f', ['п'] = 'g', ['р'] = 'h',
        ['ш'] = 'i', ['о'] = 'j', ['л'] = 'k', ['д'] = 'l',
        ['ь'] = 'm', ['т'] = 'n', ['щ'] = 'o', ['з'] = 'p',
        ['й'] = 'q', ['к'] = 'r', ['ы'] = 's', ['е'] = 't',
        ['г'] = 'u', ['м'] = 'v', ['ц'] = 'w', ['ч'] = 'x',
        ['н'] = 'y', ['я'] = 'z',
        ['э'] = '\'', ['б'] = ',', ['ё'] = '\'', ['ж'] = ';',
        ['х'] = '[', ['ъ'] = ']', ['ю'] = '.'
    };

    // EN → UK
    private static readonly Dictionary<char, char> SwitchUkFromEn = new()
    {
        ['f'] = 'а', [','] = 'б', ['<'] = 'б', ['d'] = 'в', ['u'] = 'г',
        ['l'] = 'д', ['t'] = 'е', ['`'] = 'ґ', ['~'] = 'ґ',
        [';'] = 'ж', [':'] = 'ж', ['p'] = 'з', ['b'] = 'и',
        ['q'] = 'й', ['r'] = 'к', ['k'] = 'л', ['v'] = 'м',
        ['y'] = 'н', ['j'] = 'о', ['g'] = 'п', ['h'] = 'р',
        ['c'] = 'с', ['n'] = 'т', ['e'] = 'у', ['a'] = 'ф',
        ['['] = 'х', ['{'] = 'х', ['w'] = 'ц', ['x'] = 'ч',
        ['i'] = 'ш', ['o'] = 'щ', [']'] = 'ї', ['}'] = 'ї',
        ['s'] = 'і', ['m'] = 'ь',
        ['\''] = 'є', ['"'] = 'є', ['.'] = 'ю', ['>'] = 'ю', ['z'] = 'я'
    };

    // EN → BE (like RU except: b→і, o→ў, ]→' — Belarusian has no и/щ/ъ)
    private static readonly Dictionary<char, char> SwitchBeFromEn = new()
    {
        ['f'] = 'а', [','] = 'б', ['<'] = 'б', ['d'] = 'в', ['u'] = 'г',
        ['l'] = 'д', ['t'] = 'е', ['`'] = 'ё', ['~'] = 'ё',
        [';'] = 'ж', [':'] = 'ж', ['p'] = 'з', ['b'] = 'і',
        ['q'] = 'й', ['r'] = 'к', ['k'] = 'л', ['v'] = 'м',
        ['y'] = 'н', ['j'] = 'о', ['g'] = 'п', ['h'] = 'р',
        ['c'] = 'с', ['n'] = 'т', ['e'] = 'у', ['a'] = 'ф',
        ['['] = 'х', ['{'] = 'х', ['w'] = 'ц', ['x'] = 'ч',
        ['i'] = 'ш', ['o'] = 'ў', [']'] = '\'', ['}'] = '\'',
        ['s'] = 'ы', ['m'] = 'ь',
        ['\''] = 'э', ['"'] = 'э', ['.'] = 'ю', ['>'] = 'ю', ['z'] = 'я'
    };

    // BE → EN
    private static readonly Dictionary<char, char> SwitchEnFromBe = new()
    {
        ['ф'] = 'a', ['і'] = 'b', ['с'] = 'c', ['в'] = 'd',
        ['у'] = 'e', ['а'] = 'f', ['п'] = 'g', ['р'] = 'h',
        ['ш'] = 'i', ['о'] = 'j', ['л'] = 'k', ['д'] = 'l',
        ['ь'] = 'm', ['т'] = 'n', ['ў'] = 'o', ['з'] = 'p',
        ['й'] = 'q', ['к'] = 'r', ['ы'] = 's', ['е'] = 't',
        ['г'] = 'u', ['м'] = 'v', ['ц'] = 'w', ['ч'] = 'x',
        ['н'] = 'y', ['я'] = 'z',
        ['э'] = '\'', ['б'] = ',', ['ё'] = '`', ['ж'] = ';',
        ['х'] = '[', ['ю'] = '.'
    };

    // UK → EN
    private static readonly Dictionary<char, char> SwitchEnFromUk = new()
    {
        ['ф'] = 'a', ['и'] = 'b', ['с'] = 'c', ['в'] = 'd',
        ['у'] = 'e', ['а'] = 'f', ['п'] = 'g', ['р'] = 'h',
        ['ш'] = 'i', ['о'] = 'j', ['л'] = 'k', ['д'] = 'l',
        ['ь'] = 'm', ['т'] = 'n', ['щ'] = 'o', ['з'] = 'p',
        ['й'] = 'q', ['к'] = 'r', ['і'] = 's', ['е'] = 't',
        ['г'] = 'u', ['м'] = 'v', ['ц'] = 'w', ['ч'] = 'x',
        ['н'] = 'y', ['я'] = 'z',
        ['є'] = '\'', ['б'] = ',', ['ґ'] = '`', ['ж'] = ';',
        ['х'] = '[', ['ї'] = ']', ['ю'] = '.'
    };

    private static readonly Dictionary<Lang, Dictionary<char, char>> KeyboardLayouts = new()
    {
        [Lang.Ru] = SwitchRuFromEn,
        [Lang.En] = SwitchEnFromRu,
        [Lang.Uk] = SwitchUkFromEn,
        [Lang.Be] = SwitchBeFromEn
    };

    private static readonly Dictionary<Lang, Dictionary<char, char>> KeyboardLayoutsToEn = new()
    {
        [Lang.Ru] = SwitchEnFromRu,
        [Lang.Uk] = SwitchEnFromUk,
        [Lang.Be] = SwitchEnFromBe
    };

    public static string SwitchLang(string word, Lang destinationLang)
    {
        var switchTable = KeyboardLayouts[destinationLang];
        var chars = word.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (switchTable.TryGetValue(chars[i], out char replacement))
                chars[i] = replacement;
        }
        return new string(chars);
    }

    public static string SwitchToEn(string word, Lang sourceLang)
    {
        var switchTable = KeyboardLayoutsToEn[sourceLang];
        var chars = word.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (switchTable.TryGetValue(chars[i], out char replacement))
                chars[i] = replacement;
        }
        return new string(chars);
    }

    public static Func<string, string> SwitchLangFunction(Lang destinationLang) =>
        input => SwitchLang(input, destinationLang);

    public static bool IsSeparator(char ch) => SeparatorsSet.Contains(ch);
    public static bool IsSeparatorOrPossibleRu(char ch) => SeparatorOrPossibleRuSet.Contains(ch);
    public static bool IsEnOrPossibleRu(char ch) => EnOrPossibleRuSet.Contains(ch);
    public static bool IsRuOrPossibleEn(char ch) => RuOrPossibleEnSet.Contains(ch);
    public static bool IsRuOrPossibleSeparator(char ch) => RuOrPossibleSeparatorSet.Contains(ch);
    public static bool IsPossibleRuInUppercase(char ch) => PossibleRuInUppercaseSet.Contains(ch);

    public static bool IsRussianChar(char ch) =>
        ch == 'ё' || (ch >= 'а' && ch <= 'я');

    public static bool IsRussianWord(string word)
    {
        foreach (char ch in word)
            if (!IsRussianChar(ch)) return false;
        return true;
    }

    private static readonly HashSet<char> UkrainianCharsSet = new(
        "абвгґдежзиійклмнопрстуфхцчшщьюяєії");

    public static bool IsUkrainianChar(char ch) =>
        UkrainianCharsSet.Contains(ch);

    public static bool IsUkrainianWord(string word)
    {
        foreach (char ch in word)
            if (!IsUkrainianChar(ch)) return false;
        return true;
    }

    public static bool IsEnglishChar(char ch) =>
        (ch >= 'a' && ch <= 'z') || ch == '\'';

    public static bool IsEnglishWord(string word)
    {
        foreach (char ch in word)
            if (!IsEnglishChar(ch)) return false;
        return true;
    }

    private static readonly HashSet<char> BelarusianCharsSet = new(
        "абвгдеёжзійклмнопрстуўфхцчшыьэюя");

    public static bool IsBelarusianChar(char ch) =>
        BelarusianCharsSet.Contains(ch);

    /// <summary>
    /// True when every character of <paramref name="word"/> belongs to the alphabet of
    /// <paramref name="lang"/> — i.e. the word could have been typed in that language.
    /// </summary>
    public static bool WordFitsLang(Lang lang, string word)
    {
        foreach (char ch in word)
        {
            bool fits = lang switch
            {
                Lang.Ru => IsRussianChar(ch),
                Lang.Uk => IsUkrainianChar(ch),
                Lang.Be => IsBelarusianChar(ch),
                Lang.En => IsEnglishChar(ch),
                _ => false
            };
            if (!fits) return false;
        }
        return true;
    }

    public static bool HasUkrainianSpecificChars(string word)
    {
        foreach (char ch in word)
            if (ch is 'і' or 'ї' or 'є' or 'ґ') return true;
        return false;
    }

    public static bool HasRussianSpecificChars(string word)
    {
        foreach (char ch in word)
            if (ch is 'ы' or 'э' or 'ъ' or 'ё') return true;
        return false;
    }

    public static bool IsVowel(Lang lang, char ch) => Vowels[lang].Contains(ch);
    public static bool IsConsonant(Lang lang, char ch) => Consonants[lang].Contains(ch);

    public static bool HasVowel(Lang lang, string word)
    {
        foreach (char ch in word)
            if (IsVowel(lang, ch)) return true;
        return false;
    }

    public static List<int> UppercasePositions(string str)
    {
        var positions = new List<int>(str.Length);
        for (int i = 0; i < str.Length; i++)
            if (char.IsUpper(str[i]) || IsPossibleRuInUppercase(str[i]))
                positions.Add(i);
        return positions;
    }

    public static string RestoreUppercase(string str, List<int> uppercasePositions)
    {
        if (uppercasePositions.Count == 0) return str;
        var chars = str.ToCharArray();
        foreach (int i in uppercasePositions)
            if (i < chars.Length)
                chars[i] = char.ToUpper(chars[i]);
        return new string(chars);
    }

    public static bool IsAbbreviation(string str)
    {
        if (str.Length < 3) return false;
        for (int i = 0; i < str.Length; i++)
        {
            char ch = str[i];
            if (i % 2 == 0) { if (!char.IsLetter(ch)) return false; }
            else { if (ch != '.') return false; }
        }
        return true;
    }
}
