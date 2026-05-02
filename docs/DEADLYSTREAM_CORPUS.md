# DeadlyStream archive corpus (historical reference)

All test data must be defined and constructed ephemerally in `.cs` files — in memory at test time. There are **no committed fixture files** on disk. The former `tests/KPatcher.Tests/test_files/integration_tslpatcher_archive_corpus/` directory no longer exists; all archive-corpus test behaviors are reproduced via in-memory format construction in C# test code.

## Real source listings

- [KOTOR 1 Full mod build (Neocities)](https://kotor.neocities.org/modding/mod_builds/k1/full)
- [KOTOR 2 Full mod build (Neocities)](https://kotor.neocities.org/modding/mod_builds/k2/full)

Maintainer download/organize flow for upstream archives lives under `scripts/DownloadTslpatcherModsFromBuildPages.ps1` and `scripts/OrganizeDeadlyStreamTslpatcherArchives.ps1`. These are for maintainer analysis only and are not consumed by tests.

## Tests that exercise corpus behaviors

Archive-corpus behavioral coverage is now implemented as in-memory constructed tests. Former test classes included `ArchiveCorpusFormatRoundTripTests`, `EmbeddedTslpatcherArchiveCorpusContractTests`, and per-case integration tests. All test data is built using format constructor APIs (`new GFF()`, `new TwoDA()`, `new TLK()`, etc.) or as string constants for plaintext formats.
