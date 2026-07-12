namespace RekeyNet;

/// <summary>
/// The result of analyzing a phrase: the original text, the corrected text (if the
/// keyboard layout was switched), and the individual words.
/// </summary>
public sealed class RekeyResult
{
    /// <summary>The original input text, unchanged.</summary>
    public string Original { get; }

    /// <summary>
    /// The corrected text if a wrong keyboard layout was detected and switched;
    /// <c>null</c> if no correction was needed. Use <see cref="Text"/> for the
    /// best available text without null checks.
    /// </summary>
    public string? Corrected { get; }

    /// <summary>
    /// The best available text: <see cref="Corrected"/> if a correction was applied,
    /// otherwise <see cref="Original"/>. Never <c>null</c>.
    /// </summary>
    public string Text => Corrected ?? Original;

    /// <summary><c>true</c> if a wrong layout was detected and the text was corrected.</summary>
    public bool WasCorrected => Corrected != null;

    /// <summary>The individual word tokens extracted from the input (in corrected form).</summary>
    public IReadOnlyList<string> Words { get; }

    /// <summary>
    /// How certain the correction is, as heuristic tiers (not a calibrated probability):
    /// <c>1.0</c> — nothing was corrected; <c>0.95</c> — curated exception;
    /// <c>0.9</c> — the corrected word is in the embedded known-word lists;
    /// <c>0.8</c> — the switch produced an n-gram-plausible word;
    /// <c>0.55</c> — the word was plausible in two languages and the tie was broken
    /// only by configured priority. For a phrase, the lowest word tier wins.
    /// Typical use: apply silently at ≥ 0.8, show a "did you mean …?" hint below.
    /// </summary>
    public double Confidence { get; }

    internal RekeyResult(string original, string? corrected, IReadOnlyList<string> words, double confidence = 1.0)
    {
        Original = original;
        Corrected = corrected;
        Words = words;
        Confidence = confidence;
    }

    /// <summary>Returns <see cref="Text"/>.</summary>
    public override string ToString() => Text;

    /// <summary>Implicitly converts the result to its <see cref="Text"/>.</summary>
    public static implicit operator string(RekeyResult result) => result.Text;
}
