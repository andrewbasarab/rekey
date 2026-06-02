namespace Rekey;

/// <summary>
/// Result of tokenization containing the original phrase, optional corrected phrase,
/// and list of word tokens.
/// </summary>
public sealed class TokenizerResponse
{
    /// <summary>Original input string.</summary>
    public string Original { get; }

    /// <summary>Corrected string if language switch was detected, null otherwise.</summary>
    public string? Corrected { get; }

    /// <summary>List of word tokens extracted from the input.</summary>
    public IReadOnlyList<string> Tokens { get; }

    internal TokenizerResponse(string original, string? corrected, IReadOnlyList<string> tokens)
    {
        Original = original;
        Corrected = corrected;
        Tokens = tokens;
    }

    public override string ToString() => Corrected ?? Original;
}
