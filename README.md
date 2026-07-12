# Rekey

[![CI](https://github.com/andrewbasarab/rekey/actions/workflows/ci.yml/badge.svg)](https://github.com/andrewbasarab/rekey/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Rekey.svg)](https://www.nuget.org/packages/Rekey)
[![Downloads](https://img.shields.io/nuget/dt/Rekey.svg)](https://www.nuget.org/packages/Rekey)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](LICENSE)

**Your users type `ghbdsn` when they mean `привіт` — and your search returns nothing.**

Rekey detects text typed in the **wrong keyboard layout** and rekeys it into the intended
word: a server-side [Punto Switcher](https://en.wikipedia.org/wiki/Punto_Switcher) for .NET.
One line of code, no configuration, no external services.

```csharp
var rekey = new Rekey();

rekey.Correct("ghbdsn");     // → "привіт"   (Ukrainian typed with an English layout)
rekey.Correct("xfcnbwf");    // → "частица"  (Russian typed with an English layout)
rekey.Correct("руддщ");      // → "hello"    (English typed with a Cyrillic layout)
rekey.Correct("beautiful");  // → "beautiful" (valid text passes through untouched)
```

**[▶ Try it live in your browser](https://andrewbasarab.github.io/rekey/)** — the library running as WebAssembly.

## Why you want this

Anyone who types in two layouts does it every day: they forget to switch, type
`rdbnrb` instead of `квитки` into your search box, get **zero results**, and leave.
If your audience uses Ukrainian or Russian alongside English, a real share of your
searches, filters, and autocompletes silently fail.

Rekey fixes that on the server, per request, with no UI changes:

- ⚡ **Fast** — ~880,000 words/sec on a single thread; ~15 ms one-time load
- 🪶 **Self-contained** — no dependencies, no network calls, all dictionaries embedded
- 🧵 **Thread-safe and stateless** — register one singleton and forget it
- 🌍 **English ↔ Ukrainian, Russian, and Belarusian** (BE is opt-in), both directions, mixed text, digits and case preserved
- 🛡️ **Safe by default** — `Correct()` returns the input unchanged unless the switched
  variant is actually a plausible word, and URLs, e-mails, camelCase identifiers, and
  mixed-script tokens are never touched — so you can run it on every query
- 🎚️ **Confidence score** — decide when to fix silently and when to show "did you mean …?"
- ⚖️ **Clean licensing** — Apache-2.0 code; n-gram data generated from CC0/CC BY corpora
  (see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)) — safe for commercial use

## Install

```bash
dotnet add package Rekey
```

Works everywhere: **.NET Framework 4.6.2+, .NET Core 2.0+, .NET 5+** (via `netstandard2.0`),
with optimized builds for **.NET 8** and **.NET 10**.

## Usage

```csharp
using RekeyNet;

// DI (recommended) — stateless, loads dictionaries once:
builder.Services.AddSingleton<Rekey>();

// Or without DI:
string s = Rekey.Default.Correct("ghbdsn");   // "привіт"
```

When you need details, `Analyze` returns everything:

```csharp
RekeyResult result = rekey.Analyze("ghbdsn");
result.Text;          // "привіт"  — best text, never null
result.WasCorrected;  // true      — a wrong layout was detected
result.Corrected;     // "привіт"  — null when no switch was needed
result.Original;      // "ghbdsn"
result.Words;         // ["привіт"]
result.Confidence;    // 0.9 — heuristic tiers: 1.0 untouched · 0.95 curated exception ·
                      // 0.9 known word · 0.8 plausible switch · 0.55 ambiguous tie
```

`RekeyResult` converts implicitly to `string` (yields `Text`).

Don't need one of the languages? Configure the set — and the priority — with options:

```csharp
// Ukrainian-only product: Russian corrections never appear
var rekey = new Rekey(new RekeyOptions { Languages = [Lang.En, Lang.Uk] });

// Or keep all languages but prefer Ukrainian on ambiguity:
var rekey = new Rekey(new RekeyOptions { Languages = [Lang.En, Lang.Uk, Lang.Ru] });

// Belarusian is available as an opt-in (experimental):
var rekey = new Rekey(new RekeyOptions { Languages = [Lang.En, Lang.Be] });
```

### Recipe: search that survives the wrong layout

Don't replace the user's query — **OR** the original and corrected variants, so you
match either. With PostgreSQL full-text search:

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
// "(ghbdsn:*) | (привіт:*)" — matches either spelling
```

The same pattern works for Elasticsearch, SQL `LIKE`, or any autocomplete backend.

## How it works

Rekey carries, per language, a blacklist of letter combinations ("n-grams") that never
occur in real words — e.g. no Ukrainian word contains certain consonant clusters. A token
is a plausible word in language *L* if it has a vowel and none of its n-grams are
blacklisted. For each token Rekey "retypes" the keystrokes into the other layout and keeps
the variant that looks like a real word.

One subtlety: the Russian and Ukrainian layouts differ only on four keys
(`s` `]` `'` `` ` `` → ы/ъ/э/ё vs і/ї/є/ґ), so a wrong-layout token often produces a
plausible word in *both* languages (`ghbdsn` → `привыт`/`привіт`). For those ties Rekey
embeds two compact lists of known words containing the layout-specific letters and picks
the real word.

The Ukrainian dictionaries are generated from ~378M tokens of real-world text
(ParaCrawl, CC0 + UA-GEC, CC BY 4.0) by a fully reproducible tool in
[tools/Rekey.DictGen](tools/Rekey.DictGen) — measured false-positive rate on real
Ukrainian words is 1.3%.

## Current limitations

- Languages: EN/RU/UK by default plus opt-in Belarusian. The n-gram approach ports cleanly
  to other non-Latin-script languages (Bulgarian, Greek, Hebrew, …) — open an issue if you
  need one.
- Very short tokens (1–2 letters) are inherently ambiguous; use `new Rekey(minWordLength)`
  to leave them untouched.
- Smart filtering recognizes URLs, e-mails, camelCase, and mixed-script tokens, but not
  high-entropy strings like passwords or SKUs; disable it with
  `new RekeyOptions { SmartFiltering = false }` if it gets in your way.

## Build & test

```bash
dotnet build -c Release
dotnet test
```

## Credits & license

A C# port of the Java library [blizznets/langchecker](https://github.com/blizznets/langchecker)
(Apache-2.0), extended with Ukrainian support, RU/UK disambiguation, and regenerated
dictionaries. Data sources: [ParaCrawl](https://paracrawl.eu/) (CC0 1.0),
[UA-GEC](https://github.com/grammarly/ua-gec) (CC BY 4.0) — details in
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

Licensed under [Apache-2.0](LICENSE).
