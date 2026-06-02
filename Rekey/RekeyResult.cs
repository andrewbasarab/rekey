namespace Rekey;

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

    internal RekeyResult(string original, string? corrected, IReadOnlyList<string> words)
    {
        Original = original;
        Corrected = corrected;
        Words = words;
    }

    /// <summary>Returns <see cref="Text"/>.</summary>
    public override string ToString() => Text;

    /// <summary>Implicitly converts the result to its <see cref="Text"/>.</summary>
    public static implicit operator string(RekeyResult result) => result.Text;
}
