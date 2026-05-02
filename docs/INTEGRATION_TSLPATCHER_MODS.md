# Integration TSLPatcher mod fixtures (maintainer reference)

## Policy: zero external file dependencies

All test data must be defined and constructed ephemerally in `.cs` files — in memory at test time. There must be **zero committed test fixture files** on disk. The `test_files/` directory must not exist. Only `_corrupted`-suffixed samples and `.ncs` files may be C# `byte[]` literals; all other formats must use format constructor APIs.

## Maintainer cache (not consumed by tests)

- `tests/KPatcher.Tests/test_files/integration_tslpatcher_mods/` was historically a maintainer cache for downloaded upstream archives (`deadlystream_k1`, `deadlystream_tsl`, `deadlystream_unlisted`). This directory should not exist in the committed tree.

## Download and organize (maintainer reference only)

Mod sources: [KOTOR 1 Full](https://kotor.neocities.org/modding/mod_builds/k1/full), [KOTOR 2 Full](https://kotor.neocities.org/modding/mod_builds/k2/full).

```powershell
pwsh -NoProfile -File ./scripts/DownloadTslpatcherModsFromBuildPages.ps1
pwsh -NoProfile -File ./scripts/OrganizeDeadlyStreamTslpatcherArchives.ps1
pwsh -NoProfile -File ./scripts/BuildIntegrationTslpatcherModInventory.ps1
```

These scripts are for maintainer analysis only. Tests do **not** consume their output. All mod payloads exercised by tests are constructed in C# code using format builder APIs.

## Manifest regeneration

Refresh the authoritative portal link manifests in-place with:

```powershell
pwsh -NoProfile -File ./scripts/RegeneratePortalModLinkManifests.ps1
```

This updates `.firecrawl/k1-full-links.json`, `.firecrawl/k2-full-links.json`, and `.firecrawl/portal-source-records.json`.

## Legacy download link manifest

Historical DeadlyStream / Nexus / MEGA / etc. URLs: [DEADLYSTREAM_DOWNLOAD_MANIFEST.md](DEADLYSTREAM_DOWNLOAD_MANIFEST.md).
