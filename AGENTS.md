# AGENTS.md

## Project overview

KPatcher is a C#/.NET Avalonia desktop application for installing Star Wars KOTOR mods. See `README.md` for full project details and `docs/` for additional documentation.

## Learned User Preferences

- When long-running test suites hit a wall-clock cap, treat that as a bottleneck to optimize or shard in the harness, not as a reason to disable tests or weaken assertions.
- Integration fixtures and material under `test_files` should use neutral, generic naming and commentary so nothing reads as endorsement of a specific third-party mod, storefront, or author.
- For installer and format code, prefer expanding tests with real disk I/O, committed fixtures, and explicit assertions on outputs over mocks or monkey patching.

## Learned Workspace Facts

- Large mod install integration tests should use a temporary synthetic game tree built inside the test (minimal `.mod` / capsule stubs and padded or template GFFs as needed) instead of requiring a real retail install path; bundled fixtures live under `tests/KPatcher.Tests/test_files/integration_tslpatcher_mods/`.
- INI text passed through `ConfigReader.ParseIniText` is normalized to strip a leading UTF-8 BOM (U+FEFF) so the first section line parses whether the string was produced by `ReadAllText` or by decoding bytes.
- Installer parity and gap analysis should be anchored on Stoffe's TSLPatcher official readme and TSLPatcher syntax documentation, not on HoloPatcher-specific install-phase differences, unless the project explicitly chooses to track those deltas.
- Do not frame `src/` implementation or comments around Python, PyKotor, or Python parity; keep the production codebase self-contained in terminology.

## Cursor Cloud specific instructions

### Gotchas

- **.NET SDK is installed at `$HOME/.dotnet`**, which is added to `PATH` via `~/.bashrc`. The update script also ensures this.
- **Avalonia GUI** requires `DISPLAY=:1` environment variable to launch the X11 window.
