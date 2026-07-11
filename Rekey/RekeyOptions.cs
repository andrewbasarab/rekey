namespace RekeyNet;

/// <summary>
/// Configuration for <see cref="Rekey"/>. All properties have safe defaults; new options
/// may be added in future versions without breaking existing code.
/// </summary>
public sealed class RekeyOptions
{
    /// <summary>
    /// Languages to detect and correct, in priority order: when a token is plausible in
    /// more than one language, the earlier one wins (embedded known-word lists still
    /// arbitrate RU/UK ties). Remove a language to disable it entirely — e.g.
    /// <c>[Lang.En, Lang.Uk]</c> never produces Russian corrections.
    /// Default: En, Ru, Uk.
    /// </summary>
    public IReadOnlyList<Lang> Languages { get; init; } = [Lang.En, Lang.Ru, Lang.Uk];

    /// <summary>
    /// Words shorter than this many characters are left untouched — they are too short
    /// to detect a layout reliably. Default: 0 (no minimum).
    /// </summary>
    public int MinWordLength { get; init; }
}
