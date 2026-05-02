# Exhaustive inline payloads — provenance (maintainer)

All exhaustive mod payloads are defined **entirely in C# test code** — constructed in memory at test time using format builder APIs. There are **no committed fixture files on disk**. Each payload is a 1:1 faithful reconstruction of the original mod's `tslpatchdata` tree (omitting shipped `*.exe` only).

**Binary data rules:** Only `_corrupted`-suffixed samples and `.ncs` files may be defined as C# `byte[]` literals. All other formats (GFF types like UTC/UTI/UTP/DLG/GIT/ARE, 2DA, TLK, ERF/MOD/RIM, SSF) must be **constructed using their format APIs** (`new GFF()`, `new TwoDA()`, `new ERF()`, etc.). Plaintext formats (INI, RTF, NSS source) are string constants. Top-level names are generic; do not use storefront or author branding in public docs.

## Source lists (Neocities)

- [KOTOR 1 Full mod build](https://kotor.neocities.org/modding/mod_builds/k1/full)
- [KOTOR 2 Full mod build](https://kotor.neocities.org/modding/mod_builds/k2/full)

Use [`scripts/DownloadTslpatcherModsFromBuildPages.ps1`](../scripts/DownloadTslpatcherModsFromBuildPages.ps1) (requires `.firecrawl/k1-full-links.json` and `k2-full-links.json`) then [`scripts/OrganizeDeadlyStreamTslpatcherArchives.ps1`](../scripts/OrganizeDeadlyStreamTslpatcherArchives.ps1). Zips under `integration_tslpatcher_mods/` are **maintainer cache** (gitignored); tests construct all data in memory from C# code.

When a maintainer script needs a vanilla-game source file, bootstrap a minimal KotOR root for the relevant game rather than pointing tests at a user's retail install. Real maintainer examples are `G:/SteamLibrary/Steamapps/common/swkotor` for K1 and `G:/SteamLibrary/Steamapps/common/Knights of the Old Republic II` for TSL, but the important part is the layout the script reads from. Include `chitin.key`, `dialog.tlk` when required, the expected `data/*.bif`, `Modules/` or `modules/` as appropriate, `Override/` or `override/`, and any game exe/config files the script or validator expects. K1 uses lowercase `modules/` in practice; TSL uses uppercase `Modules/`.

## Extract into a neutral payload

```powershell
pwsh -NoProfile -File ./scripts/ExtractNeutralExhaustivePayloadFromZip.ps1 `
  -ZipPath 'path/to/mod.zip' `
  -NeutralPayloadName 'multi_option_kor_gff_bundle' `
  -CleanDestination
```

The script always strips `*.exe` after extraction. Optional: `-TslpatchdataSubpath 'tslpatchdata'` when the packaged tree uses a different relative folder.

## Payload ↔ upstream mapping

Machine-readable entries: [`scripts/exhaustive_inline_payload_manifest.json`](../scripts/exhaustive_inline_payload_manifest.json).

| Neutral payload id | DeadlyStream id | Organized archive | Notes |
|--------------------|-----------------|-------------------|-------|
| `multi_option_kor_gff_bundle` | `1293` | `deadlystream_k1/1293-jcs-korriban-back-in-black-for-k1.zip` | Keep shipped `[CompileList]` inputs, modules, and option INIs from full `tslpatchdata`. |
| `namespace_main_alt_gff_bundle` | `1289` | `deadlystream_k1/1289-sith-soldier-texture-restoration.zip` | Includes `Main/`, `Alternate/`, `namespaces.ini`, modules, and Alternate Override assets. |
| `gff_git_module_texture_bundle` | `1179` | `deadlystream_k1/1179-diversified-wounded-republic-soldiers-on-taris.zip` | Includes `tar_m02ac.mod` and full Override model/texture bundle listed by `changes.ini`. |
| `heads_appearance_utc_row` | `1218` | `deadlystream_k1/1218-helena-shan-improvement.zip` | Includes heads/appearance 2DA companions plus `helena.utc` and InstallList assets. |

Keep `scripts/exhaustive_inline_payload_manifest.json` in sync when these mappings change.

## Licensing and size

Record redistribution terms per mod where required. Retail-game bootstrapping for maintainer extraction should stay minimal and local to the script workflow; tests construct all data in C# code using format builder APIs.
