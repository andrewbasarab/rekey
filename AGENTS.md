# AGENTS.md — How to use Rekey

Quick, agent-oriented reference for consuming the **Rekey** library. If you are an AI
agent integrating Rekey into another project, everything you need is here — you should not
need to read the source. (Human-oriented overview: [README.md](README.md).)

## What it does

Rekey detects text typed in the **wrong keyboard layout** and rekeys it into the intended
word. Example: a user means to type `привіт` (Ukrainian) but leaves the English layout on
and types `ghbdsn` → Rekey returns `привіт`.

- Languages: **English, Russian, Ukrainian** by default, plus opt-in **Belarusian**
  (`RekeyOptions.Languages`), both directions (EN↔RU/UK/BE).
- N-gram analysis plus two compact embedded word lists for RU↔UK disambiguation — small and fast.
- Target frameworks: **net8.0** and **net10.0** (requires .NET 8.0+). Namespace: `RekeyNet`. NuGet id: `Rekey`.

## Install

```bash
dotnet add package Rekey
```

## The entire public API

There are exactly two public types: the `Rekey` class and the `RekeyResult` it returns.

### `Rekey`

| Member | Signature | Notes |
|--------|-----------|-------|
| ctor | `new Rekey()` | Default; loads embedded dictionaries once. |
| ctor | `new Rekey(int minWordLength)` | Words shorter than `minWordLength` are left untouched. |
| ctor | `new Rekey(RekeyOptions options)` | Configure enabled languages and priority (see below). |
| static | `Rekey.Default` | Shared lazy singleton — use when you don't have DI. |
| method | `string Correct(string text)` | Returns corrected text, or the **original unchanged** if no fix was needed. |
| method | `RekeyResult Analyze(string input)` | Returns details (see below). |

### `RekeyResult`

| Member | Type | Meaning |
|--------|------|---------|
| `Text` | `string` | Best text: corrected if fixed, else original. **Never null.** |
| `WasCorrected` | `bool` | `true` if a wrong layout was detected and switched. |
| `Corrected` | `string?` | Corrected text, or **`null`** when no switch was needed. |
| `Original` | `string` | The input, unchanged. |
| `Words` | `IReadOnlyList<string>` | Word tokens in corrected form (smart-filtered tokens excluded). |
| `Confidence` | `double` | Heuristic tiers: 1.0 untouched · 0.95 curated exception · 0.9 known word · 0.8 plausible switch · 0.55 ambiguous tie. Apply silently at ≥ 0.8; hint below. |

`RekeyResult` has an implicit `string` conversion (yields `Text`), so it can be used
directly where a `string` is expected.

## Usage

```csharp
using RekeyNet;

var rekey = new Rekey();

// Simple — string in, string out:
string fixedText = rekey.Correct("ghbdsn");   // "привіт"
rekey.Correct("beautiful");                   // "beautiful" (already valid → unchanged)

// Detailed:
RekeyResult result = rekey.Analyze("ghbdsn");
result.Text;          // "привіт"  (never null)
result.WasCorrected;  // true
result.Corrected;     // "привіт"  (null when nothing changed)
result.Original;      // "ghbdsn"
result.Words;         // ["привіт"]
```

## Critical integration rules

- **Reuse one instance.** `Rekey` is stateless and thread-safe but loads dictionaries on
  construction. Never `new Rekey()` per request.
  - With DI: `builder.Services.AddSingleton<Rekey>();`
  - Without DI: `Rekey.Default`
- **`Correct` is non-destructive.** It returns the original string unchanged when no fix is
  needed, so it is safe to call on every input. Don't guard it with your own heuristics.
- **Check `WasCorrected` (or `Corrected != null`)** before treating the result as a change.
  Use `Corrected!` (non-null) only inside that branch; use `Text` everywhere else.
- **Languages are configurable.** Default is all three with Russian > Ukrainian priority
  for ambiguous Cyrillic. Uniquely Ukrainian (`і ї є ґ`) or Russian (`ы э ъ ё`) characters
  override priority, and when a token switches to a plausible word in both languages,
  embedded known-word lists pick the real word. To disable a language or change priority:
  `new Rekey(new RekeyOptions { Languages = [Lang.En, Lang.Uk] })` — order = priority,
  omitted languages never appear in corrections.
- **Smart filtering is on by default.** URLs (`://`, `www.`), e-mails, camelCase/PascalCase
  identifiers, and mixed Latin+Cyrillic tokens are never corrected and are excluded from
  `Words`. It does NOT recognize passwords/SKUs (high-entropy strings) — pre-filter those
  yourself. Disable with `new RekeyOptions { SmartFiltering = false }`.

## Common pattern: PostgreSQL full-text search

OR the original and corrected queries so search works even with the wrong layout:

```csharp
var result = rekey.Analyze(q);
var query = string.Join(" & ", terms.Select(t => $"{t}:*"));

if (result.WasCorrected)
{
    var corrected = Regex.Split(result.Corrected!, @"\W+")
        .Where(t => !string.IsNullOrWhiteSpace(t))
        .Select(t => $"{t.ToLowerInvariant()}:*");
    query = $"({query}) | ({string.Join(" & ", corrected)})";
}
// "(ghbdsn:*) | (привіт:*)" — PostgreSQL matches either
```

## Build & test (when working inside this repo)

```bash
dotnet build -c Release   # produces the .nupkg (GeneratePackageOnBuild)
dotnet test               # runs the xUnit suite in Rekey.Tests
```
