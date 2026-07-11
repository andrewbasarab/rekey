# Rekey.DictGen

Regenerates the Ukrainian data files in `Rekey/Resources` from scratch, so the
library carries **no data derived from restrictively-licensed dictionaries**.

Generated files:

| File | What it is |
|------|------------|
| `nonexistent2gram-uk.txt` | 2-letter combos never at word start (`*xy`), end (`xy*`), or interior (`xy`) |
| `nonexistent3gram-uk.txt` | vowel-only trigrams never seen in a word |
| `nonexistentFirst4gram-uk.txt` | 4-char prefixes of EN→UK switched English words that no real Ukrainian word starts with |
| `nonexistent4gram-uk.txt` | all-consonant 4-grams of switched English words never seen in a real word |
| `knownwords-uk.txt` | frequent words containing і/ї/є/ґ — runtime RU/UK tie-break |
| `knownwords-ru.txt` | words containing ы/э/ъ/ё (from Apache-2.0 `words-ru.txt`) — runtime RU/UK tie-break |
| `Rekey.Tests/Resources/words-uk.txt` | flat corpus word list kept for benchmarking |

## Corpus sources (clean licenses only)

1. **ParaCrawl v9, Ukrainian mono** — CC0 1.0 (~1.6 GB gzip, ~378M tokens):
   `https://object.pouta.csc.fi/OPUS-ParaCrawl/v9/mono/uk.txt.gz`
2. **UA-GEC** — CC BY 4.0, use the corrected (`target`) side only:
   `https://github.com/grammarly/ua-gec` → concatenate `data/*/*/target/*.txt`

## Regenerating

```bash
cd tools/Rekey.DictGen

# 1. Word frequencies (streams .gz directly; keeps only pure-Ukrainian tokens)
dotnet run -c Release -- extract uk-words.tsv paracrawl-uk.txt.gz ua-gec-target.txt

# 2. N-gram dictionaries (minFreq=10, boundaryMinFreq=100 — see below)
dotnet run -c Release -- generate uk-words.tsv ../../Rekey.Tests/Resources/words-en.txt ../../Rekey/Resources 10 100

# 3. Runtime tie-break word lists
dotnet run -c Release -- knownwords-uk uk-words.tsv ../../Rekey/Resources/knownwords-uk.txt 50
dotnet run -c Release -- knownwords-ru ../../Rekey.Tests/Resources/words-ru.txt ../../Rekey/Resources/knownwords-ru.txt

# 4. Test word list
dotnet run -c Release -- wordlist uk-words.tsv ../../Rekey.Tests/Resources/words-uk.txt 10

# 5. Sanity check: % of real corpus words wrongly rejected (expect ~1.3%)
dotnet run -c Release -- validate uk-words.tsv ../../Rekey/Resources 570000
```

## Thresholds

- `minFreq=10` — a word must occur ≥10 times to count as evidence that its
  n-grams exist. Filters typos and OCR junk out of the web corpus.
- `boundaryMinFreq=100` — word-boundary evidence (start/end 2-grams, 4-char
  prefixes) needs ≥100 occurrences: rare transliterated foreign names (e.g.
  "цшерндорф" ← Zscherndorf) would otherwise legitimize impossible word starts.
- `knownwords-uk` uses `minFreq=50` to keep the embedded list compact (~54k words).

Reference metrics measured on ParaCrawl (higher is better for detection,
lower for false positives):

| Dictionaries | False positives (top 570k real words) | Switched-EN detection |
|---|---|---|
| These (10/100) | **1.30%** | 99.05% |
| Old dict_uk-derived | 2.67% | 98.52% |
