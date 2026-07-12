# Rekey.DictGen

Regenerates the per-language data files in `Rekey/Resources` from scratch, so the
library carries **no data derived from restrictively-licensed dictionaries**.

Generated files per language (`{lang}` = `uk`, `be`):

| File | What it is |
|------|------------|
| `nonexistent2gram-{lang}.txt` | 2-letter combos never at word start (`*xy`), end (`xy*`), or interior (`xy`) |
| `nonexistent3gram-{lang}.txt` | vowel-only trigrams never seen in a word |
| `nonexistentFirst4gram-{lang}.txt` | 4-char prefixes of switched English words that no real word starts with |
| `nonexistent4gram-{lang}.txt` | all-consonant 4-grams of switched English words never seen in a real word |
| `knownwords-{lang}.txt` | frequent words containing layout-distinctive letters — runtime RU/UK/BE tie-break |
| `Rekey.Tests/Resources/words-{lang}.txt` | flat corpus word list used by the corpus-quality gate tests |

(`knownwords-ru.txt` comes from the Apache-2.0 `words-ru.txt` via `knownwords-ru`.)

## Corpus sources (clean licenses only)

- **Ukrainian**: ParaCrawl v9 mono, CC0 1.0 (~378M tokens):
  `https://object.pouta.csc.fi/OPUS-ParaCrawl/v9/mono/uk.txt.gz`
  plus UA-GEC (CC BY 4.0), corrected `target` side: `https://github.com/grammarly/ua-gec`
- **Belarusian**: HPLT 2.0 cleaned mono, CC0 1.0 (~1.06B tokens):
  `https://data.hplt-project.org/two/cleaned/bel_Cyrl/1.jsonl.zst`

## Regenerating (example: Belarusian)

```bash
cd tools/Rekey.DictGen

# 1. Word frequencies (streams .gz/.zst directly; keeps only pure-alphabet tokens)
dotnet run -c Release -- extract be be-words.tsv hplt-be-1.jsonl.zst

# 2. N-gram dictionaries
dotnet run -c Release -- generate be be-words.tsv ../../Rekey.Tests/Resources/words-en.txt ../../Rekey/Resources 10 100

# 3. Runtime tie-break word list (BE uses minFreq=500 to keep it compact)
dotnet run -c Release -- knownwords be be-words.tsv ../../Rekey/Resources/knownwords-be.txt 500

# 4. Test word list for the corpus-quality gate
dotnet run -c Release -- wordlist be-words.tsv ../../Rekey.Tests/Resources/words-be.txt 100

# 5. Sanity check: % of real corpus words wrongly rejected
dotnet run -c Release -- validate be be-words.tsv ../../Rekey/Resources 570000
```

For Ukrainian replace `be` with `uk` (knownwords minFreq=50, wordlist minFreq=10).

## Thresholds

- `minFreq=10` — a word must occur ≥10 times to count as evidence that its n-grams
  exist. Filters typos and OCR junk out of web corpora.
- `boundaryMinFreq=100` — word-boundary evidence (start/end 2-grams, 4-char prefixes)
  needs ≥100 occurrences: rare transliterated foreign names (e.g. "цшерндорф" ←
  Zscherndorf) would otherwise legitimize impossible word starts.
- `knownwords` thresholds are tuned per language to keep the embedded lists compact
  (uk: 50 → ~54k words; be: 500 → ~77k words — і/ы are extremely common in Belarusian).

## Reference metrics (July 2026)

| Language | False positives (checker, top 570k real words) | Round-trip FP (`Correct()`) | Switched-EN detection |
|---|---|---|---|
| uk (these, 10/100) | 1.30% | 0.224% | 99.05% |
| uk (old dict_uk-derived) | 2.67% | — | 98.52% |
| be (10/100) | 0.52% | 0.092% | 98.24% |

The corpus-quality gate tests in `Rekey.Tests/CorpusQualityTests.cs` enforce the
round-trip numbers stay below fixed limits.
