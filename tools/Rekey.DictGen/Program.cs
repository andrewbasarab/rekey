using System.IO.Compression;
using System.Text;

// Rekey.DictGen — regenerates the Ukrainian "nonexistent n-gram" dictionaries in
// Rekey/Resources from a plain-text corpus. See tools/Rekey.DictGen/README.md for
// corpus sources and the exact commands.
//
// Methodology (mirrors NgramLangChecker.Check semantics):
//   2-grams  : exhaustive 33x33 alphabet pairs; "*xy" = never starts a word,
//              "xy*" = never ends a word, "xy" = never at interior positions
//              (offsets 1..len-3, matching the checker's middle scan).
//   3-grams  : exhaustive vowel triples (10^3) never seen as a substring.
//   First-4  : first 4 chars of English words switched EN->UK that never occur
//              as the first 4 chars of a real Ukrainian word.
//   4-grams  : all-consonant 4-char windows of switched English words that never
//              occur as an all-consonant substring of a real Ukrainian word.

const string Alphabet = "абвгґдежзиіїйклмнопрстуфхцчшщьюяє";
const string Vowels = "аеєиіїоуюя";
const string Consonants = "бвгґджзйклмнпрстфхцчшщь";

// EN -> UK keyboard layout (letter keys + apostrophe), must match Characters.SwitchUkFromEn.
var enToUk = new Dictionary<char, char>
{
    ['a'] = 'ф', ['b'] = 'и', ['c'] = 'с', ['d'] = 'в', ['e'] = 'у', ['f'] = 'а',
    ['g'] = 'п', ['h'] = 'р', ['i'] = 'ш', ['j'] = 'о', ['k'] = 'л', ['l'] = 'д',
    ['m'] = 'ь', ['n'] = 'т', ['o'] = 'щ', ['p'] = 'з', ['q'] = 'й', ['r'] = 'к',
    ['s'] = 'і', ['t'] = 'е', ['u'] = 'г', ['v'] = 'м', ['w'] = 'ц', ['x'] = 'ч',
    ['y'] = 'н', ['z'] = 'я', ['\''] = 'є'
};

var alphabetSet = Alphabet.ToHashSet();
var vowelSet = Vowels.ToHashSet();
var consonantSet = Consonants.ToHashSet();

return args switch
{
    ["extract", var output, .. var inputs] when inputs.Length > 0 => Extract(inputs, output),
    ["generate", var wordsTsv, var wordsEn, var outDir, .. var rest] => Generate(
        wordsTsv, wordsEn, outDir,
        rest.Length > 0 ? int.Parse(rest[0]) : 10,
        rest.Length > 1 ? int.Parse(rest[1]) : 200),
    ["wordlist", var wordsTsv, var output, .. var rest] =>
        WordList(wordsTsv, output, rest.Length > 0 ? int.Parse(rest[0]) : 10),
    ["knownwords-uk", var wordsTsv, var output, .. var rest] =>
        KnownWordsUk(wordsTsv, output, rest.Length > 0 ? int.Parse(rest[0]) : 50),
    ["knownwords-ru", var wordsRu, var output] => KnownWordsRu(wordsRu, output),
    ["validate", var wordsTsv, var resourceDir, .. var rest] =>
        Validate(wordsTsv, resourceDir, rest.Length > 0 ? int.Parse(rest[0]) : 50000),
    _ => Usage()
};

int Usage()
{
    Console.Error.WriteLine("""
        Usage:
          extract  <output.tsv> <corpus.txt[.gz]>...      tokenize corpora -> word<TAB>count
          generate <words.tsv> <words-en.txt> <outDir> [minFreq=10] [boundaryMinFreq=200]
          wordlist <words.tsv> <output.txt> [minFreq=10]  flat word list (test resource)
          knownwords-uk <words.tsv> <output.txt> [minFreq=50]  words with і/ї/є/ґ (RU/UK tie-break)
          knownwords-ru <words-ru.txt> <output.txt>            words with ы/э/ъ/ё (RU/UK tie-break)
          validate <words.tsv> <resourceDir> [topN=50000] false-positive rate of generated dicts
        """);
    return 2;
}

int Extract(string[] inputs, string output)
{
    var counts = new Dictionary<string, long>(1 << 22);
    long totalTokens = 0;
    var token = new StringBuilder(64);

    foreach (var input in inputs)
    {
        Console.WriteLine($"Reading {input}...");
        using var raw = File.OpenRead(input);
        using Stream stream = input.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)
            ? new GZipStream(raw, CompressionMode.Decompress)
            : raw;
        using var reader = new StreamReader(stream, Encoding.UTF8);

        int ch;
        token.Clear();
        while ((ch = reader.Read()) != -1)
        {
            var c = (char)ch;
            // Keep whole lexical tokens together (letters incl. apostrophes/hyphen)
            // so that e.g. Russian words are dropped entirely instead of being
            // split into fragments that would pollute the Ukrainian word list.
            if (char.IsLetter(c) || c is '\'' or '’' or 'ʼ' or '-')
            {
                token.Append(char.ToLowerInvariant(c));
                if (token.Length > 64) token.Clear(); // runaway garbage
            }
            else if (token.Length > 0)
            {
                CountToken(token, counts, alphabetSet, ref totalTokens);
                token.Clear();
            }
        }
        if (token.Length > 0)
            CountToken(token, counts, alphabetSet, ref totalTokens);
        Console.WriteLine($"  cumulative: {totalTokens:N0} Ukrainian tokens, {counts.Count:N0} unique");
    }

    using var writer = new StreamWriter(output, false, new UTF8Encoding(false));
    foreach (var (word, count) in counts.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal))
        writer.WriteLine($"{word}\t{count}");
    Console.WriteLine($"Wrote {counts.Count:N0} words to {output}");
    return 0;

    static void CountToken(StringBuilder token, Dictionary<string, long> counts, HashSet<char> alphabet, ref long total)
    {
        if (token.Length is < 1 or > 32) return;
        foreach (var chunk in token.GetChunks())
            foreach (var c in chunk.Span)
                if (!alphabet.Contains(c))
                    return; // not a pure Ukrainian word (incl. apostrophes/hyphens) — skip
        var word = token.ToString();
        counts[word] = counts.GetValueOrDefault(word) + 1;
        total++;
    }
}

List<string> LoadWords(string wordsTsv, int minFreq)
{
    return LoadWordCounts(wordsTsv, minFreq).Select(wc => wc.Word).ToList();
}

List<(string Word, long Count)> LoadWordCounts(string wordsTsv, int minFreq)
{
    var words = new List<(string, long)>(1 << 20);
    foreach (var line in File.ReadLines(wordsTsv))
    {
        var tab = line.IndexOf('\t');
        if (tab <= 0) continue;
        var count = long.Parse(line.AsSpan(tab + 1));
        if (count < minFreq) continue;
        words.Add((line[..tab], count));
    }
    return words;
}

// Word-boundary evidence (start/end 2-grams, 4-char prefixes) uses a stricter
// frequency threshold than word-interior evidence: a web corpus contains rare
// transliterated foreign names ("цшерндорф") that would otherwise legitimize
// impossible word starts/endings.
(HashSet<string> Start2, HashSet<string> End2, HashSet<string> Mid2,
 HashSet<string> Vowel3, HashSet<string> Prefix4, HashSet<string> Cons4)
    BuildEvidence(List<(string Word, long Count)> words, long boundaryMinFreq)
{
    var start2 = new HashSet<string>();
    var end2 = new HashSet<string>();
    var mid2 = new HashSet<string>();
    var vowel3 = new HashSet<string>();
    var prefix4 = new HashSet<string>();
    var cons4 = new HashSet<string>();

    foreach (var (w, count) in words)
    {
        int len = w.Length;
        if (len >= 2 && count >= boundaryMinFreq)
        {
            start2.Add(w[..2]);
            end2.Add(w[(len - 2)..]);
        }
        // Interior 2-grams: same window as NgramLangChecker's middle scan (i in 1..len-3).
        for (int i = 1; i < len - 2; i++)
            mid2.Add(w.Substring(i, 2));

        for (int i = 0; i + 3 <= len; i++)
        {
            if (vowelSet.Contains(w[i]) && vowelSet.Contains(w[i + 1]) && vowelSet.Contains(w[i + 2]))
                vowel3.Add(w.Substring(i, 3));
        }

        if (len >= 4 && count >= boundaryMinFreq)
            prefix4.Add(w[..4]);

        for (int i = 0; i + 4 <= len; i++)
        {
            if (consonantSet.Contains(w[i]) && consonantSet.Contains(w[i + 1])
                && consonantSet.Contains(w[i + 2]) && consonantSet.Contains(w[i + 3]))
                cons4.Add(w.Substring(i, 4));
        }
    }
    return (start2, end2, mid2, vowel3, prefix4, cons4);
}

int Generate(string wordsTsv, string wordsEnPath, string outDir, int minFreq, int boundaryMinFreq)
{
    Console.WriteLine($"Loading corpus words (minFreq={minFreq}, boundaryMinFreq={boundaryMinFreq})...");
    var words = LoadWordCounts(wordsTsv, minFreq);
    Console.WriteLine($"  {words.Count:N0} words");

    var (start2, end2, mid2, vowel3, prefix4, cons4) = BuildEvidence(words, boundaryMinFreq);

    // 2-grams: exhaustive alphabet x alphabet.
    var gram2 = new List<string>();
    foreach (var a in Alphabet)
        foreach (var b in Alphabet)
        {
            var g = $"{a}{b}";
            if (!start2.Contains(g)) gram2.Add("*" + g);
            if (!end2.Contains(g)) gram2.Add(g + "*");
            if (!mid2.Contains(g)) gram2.Add(g);
        }

    // 3-grams: exhaustive vowel^3.
    var gram3 = new List<string>();
    foreach (var a in Vowels)
        foreach (var b in Vowels)
            foreach (var c in Vowels)
            {
                var g = $"{a}{b}{c}";
                if (!vowel3.Contains(g)) gram3.Add(g);
            }

    // Candidates from English words switched to the Ukrainian layout.
    var first4 = new SortedSet<string>(StringComparer.Ordinal);
    var gram4 = new SortedSet<string>(StringComparer.Ordinal);
    var buffer = new char[128];
    foreach (var line in File.ReadLines(wordsEnPath))
    {
        var en = line.Trim().ToLowerInvariant();
        if (en.Length < 4 || en.Length > buffer.Length) continue;

        Span<char> uk = buffer.AsSpan(0, en.Length);
        bool ok = true;
        for (int i = 0; i < en.Length; i++)
        {
            if (!enToUk.TryGetValue(en[i], out uk[i])) { ok = false; break; }
        }
        if (!ok) continue;

        var prefix = new string(uk[..4]);
        if (!prefix4.Contains(prefix)) first4.Add(prefix);

        for (int i = 0; i + 4 <= uk.Length; i++)
        {
            if (consonantSet.Contains(uk[i]) && consonantSet.Contains(uk[i + 1])
                && consonantSet.Contains(uk[i + 2]) && consonantSet.Contains(uk[i + 3]))
            {
                var g = new string(uk.Slice(i, 4));
                if (!cons4.Contains(g)) gram4.Add(g);
            }
        }
    }

    Directory.CreateDirectory(outDir);
    WriteLines(Path.Combine(outDir, "nonexistent2gram-uk.txt"), gram2.OrderBy(s => s, StringComparer.Ordinal));
    WriteLines(Path.Combine(outDir, "nonexistent3gram-uk.txt"), gram3);
    WriteLines(Path.Combine(outDir, "nonexistentFirst4gram-uk.txt"), first4);
    WriteLines(Path.Combine(outDir, "nonexistent4gram-uk.txt"), gram4);

    Console.WriteLine($"nonexistent2gram-uk.txt      : {gram2.Count:N0}");
    Console.WriteLine($"nonexistent3gram-uk.txt      : {gram3.Count:N0}");
    Console.WriteLine($"nonexistentFirst4gram-uk.txt : {first4.Count:N0}");
    Console.WriteLine($"nonexistent4gram-uk.txt      : {gram4.Count:N0}");
    return 0;

    static void WriteLines(string path, IEnumerable<string> lines)
    {
        using var writer = new StreamWriter(path, false, new UTF8Encoding(false)) { NewLine = "\n" };
        foreach (var line in lines)
            writer.WriteLine(line);
    }
}

int WordList(string wordsTsv, string output, int minFreq)
{
    var words = LoadWords(wordsTsv, minFreq);
    words.Sort(StringComparer.Ordinal);
    using var writer = new StreamWriter(output, false, new UTF8Encoding(false)) { NewLine = "\n" };
    foreach (var w in words)
        writer.WriteLine(w);
    Console.WriteLine($"Wrote {words.Count:N0} words (minFreq={minFreq}) to {output}");
    return 0;
}

// Known Ukrainian words containing UK-specific letters (і/ї/є/ґ). Used at runtime to
// arbitrate RU vs UK when a wrong-layout token switches to a valid word in BOTH languages
// (the layouts differ only on the s/]/'/` keys -> ы/ъ/э/ё vs і/ї/є/ґ).
int KnownWordsUk(string wordsTsv, string output, int minFreq)
{
    var specific = new[] { 'і', 'ї', 'є', 'ґ' };
    var words = LoadWords(wordsTsv, minFreq)
        .Where(w => w.Length >= 2 && w.IndexOfAny(specific) >= 0)
        .OrderBy(w => w, StringComparer.Ordinal);
    WriteList(output, words);
    return 0;
}

// Known Russian words containing RU-specific letters (ы/э/ъ/ё), extracted from the
// Apache-2.0 words-ru.txt list of the original langchecker project.
int KnownWordsRu(string wordsRuPath, string output)
{
    var specific = new[] { 'ы', 'э', 'ъ', 'ё' };
    var words = File.ReadLines(wordsRuPath)
        .Select(l => l.Trim().ToLowerInvariant())
        .Where(w => w.Length >= 2 && w.IndexOfAny(specific) >= 0)
        .Distinct()
        .OrderBy(w => w, StringComparer.Ordinal);
    WriteList(output, words);
    return 0;
}

void WriteList(string output, IEnumerable<string> words)
{
    int n = 0;
    using var writer = new StreamWriter(output, false, new UTF8Encoding(false)) { NewLine = "\n" };
    foreach (var w in words)
    {
        writer.WriteLine(w);
        n++;
    }
    Console.WriteLine($"Wrote {n:N0} words to {output}");
}

// Re-implements NgramLangChecker.Check(Lang.Uk, word) against the generated files and
// reports how many real corpus words would be rejected (false positives).
int Validate(string wordsTsv, string resourceDir, int topN)
{
    var gram2 = File.ReadAllLines(Path.Combine(resourceDir, "nonexistent2gram-uk.txt")).ToHashSet();
    var gram3 = File.ReadAllLines(Path.Combine(resourceDir, "nonexistent3gram-uk.txt")).ToHashSet();
    var first4 = File.ReadAllLines(Path.Combine(resourceDir, "nonexistentFirst4gram-uk.txt")).ToHashSet();
    var gram4 = File.ReadAllLines(Path.Combine(resourceDir, "nonexistent4gram-uk.txt")).ToHashSet();

    var words = LoadWords(wordsTsv, 1).Take(topN).ToList();
    int rejected = 0;
    var samples = new List<string>();
    foreach (var w in words)
    {
        if (!Check(w))
        {
            rejected++;
            if (samples.Count < 25) samples.Add(w);
        }
    }
    Console.WriteLine($"Rejected {rejected:N0} of top {words.Count:N0} corpus words ({100.0 * rejected / words.Count:F3}%)");
    if (samples.Count > 0)
        Console.WriteLine("Samples: " + string.Join(", ", samples));
    return 0;

    bool Check(string word)
    {
        int len = word.Length;
        if (!word.Any(vowelSet.Contains)) return false;
        if (len >= 6 && FirstRun(word, 6, consonantSet) != null) return false;
        if (len >= 4 && first4.Contains(word[..4])) return false;
        if (len >= 3 && FirstRun(word, 3, vowelSet) is { } v3 && gram3.Contains(v3)) return false;
        if (len >= 4 && FirstRun(word, 4, consonantSet) is { } c4 && gram4.Contains(c4)) return false;
        if (len >= 2 && (gram2.Contains("*" + word[..2]) || gram2.Contains(word[(len - 2)..] + "*"))) return false;
        if (len >= 4)
            for (int i = 1; i < len - 2; i++)
                if (gram2.Contains(word.Substring(i, 2)))
                    return false;
        return true;
    }

    static string? FirstRun(string word, int n, HashSet<char> set)
    {
        for (int begin = 0; begin + n <= word.Length; begin++)
        {
            bool all = true;
            for (int i = begin; i < begin + n; i++)
                if (!set.Contains(word[i])) { all = false; break; }
            if (all) return word.Substring(begin, n);
        }
        return null;
    }
}
