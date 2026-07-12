using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using RekeyNet;

BenchmarkRunner.Run<CorrectBenchmarks>(args: args);

[MemoryDiagnoser]
public class CorrectBenchmarks
{
    private readonly Rekey _rekey = new();
    private string[] _words = [];

    [GlobalSetup]
    public void Setup()
    {
        // Mixed real-word corpus: valid words dominate, as in production traffic.
        var root = FindRepoRoot();
        _words = File.ReadAllLines(Path.Combine(root, "Rekey.Tests", "Resources", "words-uk.txt"))
            .Concat(File.ReadAllLines(Path.Combine(root, "Rekey.Tests", "Resources", "words-en.txt")))
            .Concat(File.ReadAllLines(Path.Combine(root, "Rekey.Tests", "Resources", "words-ru.txt")))
            .Where(w => w.Length > 1)
            .ToArray();
    }

    /// <summary>Throughput over ~500k real words (the words/sec headline number).</summary>
    [Benchmark]
    public int CorrectHalfMillionWords()
    {
        int corrected = 0;
        foreach (var word in _words)
        {
            if (!ReferenceEquals(_rekey.Correct(word), word))
                corrected++;
        }
        return corrected;
    }

    [Benchmark]
    public string ValidEnglishWord() => _rekey.Correct("beautiful");

    [Benchmark]
    public string SwitchedUkrainianWord() => _rekey.Correct("ghbdsn");

    [Benchmark]
    public RekeyResult AnalyzeMixedPhrase() => _rekey.Analyze("ghbdsn beautiful test@ukr.net частица");

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Rekey.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Rekey.sln not found above " + AppContext.BaseDirectory);
    }
}
