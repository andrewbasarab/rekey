using System.Reflection;

namespace RekeyNet.Tests;

/// <summary>
/// Regression gate: real words from the reference corpora must pass through
/// <see cref="Rekey.Correct"/> unchanged. Any dictionary or algorithm change that
/// starts "correcting" valid words fails these limits (measured baseline July 2026:
/// uk 0.224%, ru 0.002%, en 0.052%).
/// </summary>
public class CorpusQualityTests
{
    [Theory]
    [InlineData("words-uk.txt", 0.5)]
    [InlineData("words-ru.txt", 0.05)]
    [InlineData("words-en.txt", 0.2)]
    public void RealWordsAreLeftAlone(string resource, double maxFalsePositivePercent)
    {
        var rekey = new Rekey();
        var words = ReadWords(resource);

        int changed = 0;
        var samples = new List<string>();
        foreach (var word in words)
        {
            var corrected = rekey.Correct(word);
            if (corrected != word)
            {
                changed++;
                if (samples.Count < 10)
                    samples.Add($"{word} -> {corrected}");
            }
        }

        double percent = 100.0 * changed / words.Count;
        Assert.True(percent <= maxFalsePositivePercent,
            $"{resource}: {changed:N0} of {words.Count:N0} real words were \"corrected\" " +
            $"({percent:F3}%, limit {maxFalsePositivePercent}%). Samples: {string.Join(", ", samples)}");
    }

    private static List<string> ReadWords(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .Single(n => n.EndsWith(fileName, StringComparison.Ordinal));

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);

        var words = new List<string>();
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.Length > 1)
                words.Add(line);
        }
        return words;
    }
}
