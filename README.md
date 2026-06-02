# Rekey

Detects text typed in the **wrong keyboard layout** and corrects it — a server-side
[Punto Switcher](https://en.wikipedia.org/wiki/Punto_Switcher) for .NET.

Type `ghbdsn` with an English layout when you meant Ukrainian? Rekey turns it back into `привіт`.

Supports **English, Russian, and Ukrainian**, in both directions (EN↔RU, EN↔UK).
It uses n-gram analysis, so there is **no word list at runtime** — it's small and fast.

## Install

```bash
dotnet add package Rekey
```

## Usage

```csharp
using Rekey;

var rekey = new Rekey();

// Simple — string in, string out (returns the original if nothing needs fixing):
string fixedText = rekey.Correct("ghbdsn");   // "привіт"
rekey.Correct("beautiful");                   // "beautiful" (already valid)

// Detailed — when you need to know what happened:
RekeyResult result = rekey.Analyze("ghbdsn");
result.Text;          // "привіт"  — best text, never null
result.WasCorrected;  // true
result.Corrected;     // "привіт"  — null when no switch was needed
result.Original;      // "ghbdsn"
result.Words;         // ["привіт"]
```

`Rekey` is stateless and thread-safe. It loads its dictionaries once, so reuse a single
instance:

```csharp
// Dependency injection — register as a singleton:
builder.Services.AddSingleton<Rekey>();

// No DI? Use the shared instance:
string s = Rekey.Default.Correct("ghbdsn");
```

### Example: PostgreSQL full-text search

Search still works when the user forgot to switch layout — OR the original and corrected queries:

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

## How it works

Rekey never stores a list of valid words. Instead it carries, per language, a blacklist of
letter combinations ("n-grams") that never occur in real words. A token is treated as a
plausible word in language *L* if it has a vowel and none of its n-grams are blacklisted.
For each word it decides whether it looks like Latin or Cyrillic, "retypes" the keystrokes
into the other layout, and keeps the variant that looks like a real word.

## Build & test

```bash
dotnet build -c Release
dotnet test
```

## Credits & license

A C# port of the Java library [blizznets/langchecker](https://github.com/blizznets/langchecker),
extended with Ukrainian support. Ukrainian n-gram data is derived from
[brown-uk/dict_uk](https://github.com/brown-uk/dict_uk).

Licensed under [Apache-2.0](https://www.apache.org/licenses/LICENSE-2.0).
