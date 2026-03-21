# Firecrawl CLI + KPatcher `.env`

The Firecrawl CLI reads **`FIRECRAWL_API_KEY`** and **`FIRECRAWL_API_URL`** from the process environment. It does **not** auto-load a repo `.env` file. This repo adds a thin wrapper so **KPatcher’s root `.env` is always the source of truth**, from any working directory.

## One-time setup (recommended)

1. Keep secrets in **`KPatcher/.env`** (already gitignored). Example shape:

   ```env
   FIRECRAWL_API_KEY="your-key"
   FIRECRAWL_API_URL="https://your-firecrawl-api-host"
   ```

2. **Prepend** `KPatcher/scripts` to your **user** `PATH` so `firecrawl` resolves to the wrapper before npm’s global binary:

   ```powershell
   cd C:\GitHub\KPatcher
   powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Add-FirecrawlEnvToUserPath.ps1
   ```

3. **Close and reopen** terminals (and Cursor) so new `PATH` is visible everywhere.

4. Verify:

   ```powershell
   cd $env:TEMP
   where.exe firecrawl
   firecrawl --status
   ```

   The first `where` line should be under `...\KPatcher\scripts\firecrawl.cmd`.

## How it works

| Piece | Role |
|--------|------|
| `scripts/firecrawl.cmd` | Windows `PATH` entry; calls `firecrawl.ps1`. |
| `scripts/firecrawl.ps1` | Loads `.env`, then runs the **next** `firecrawl` on `PATH` (skips `scripts/`). |
| `scripts/Import-KPatcherFirecrawlEnv.ps1` | Parses `.env` into **process** env vars; safe to dot-source. |

### Resolving which `.env` file

1. `FIRECRAWL_ENV_FILE` — full path to any `.env` (overrides everything).
2. `KPATCHER_ROOT` — use `%KPATCHER_ROOT%\.env`.
3. Default — `<repo>\.env` where `repo` is the parent of `scripts/`.

So the file moves with the clone; no hardcoded drive letters unless you set `KPATCHER_ROOT` / `FIRECRAWL_ENV_FILE`.

## Optional: PowerShell profile hook

If you want **npm’s** `firecrawl` (without the wrapper) to still see the same variables in **interactive** PowerShell:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Install-FirecrawlProfileHook.ps1
```

That appends a line to `$PROFILE` that dot-sources `Import-KPatcherFirecrawlEnv.ps1`. Use **either** PATH prepend **or** profile hook, or both (harmless; env values match).

## Other shells / CI / Task Scheduler

- **cmd.exe**: Rely on `firecrawl.cmd` after PATH prepend, or run  
  `powershell -NoProfile -ExecutionPolicy Bypass -File C:\path\to\KPatcher\scripts\firecrawl.ps1 ...`
- **Git Bash**: Prefer `cmd //c firecrawl ...` or call `firecrawl.ps1` via PowerShell; bash does not load these wrappers natively.
- **CI**: Set `FIRECRAWL_ENV_FILE` to the checked-out `.env` path, or inject `FIRECRAWL_API_KEY` / `FIRECRAWL_API_URL` as secret variables (do not commit `.env`).

## Clearing conflicting Firecrawl config

The CLI may cache login under `%APPDATA%\firecrawl-cli`. Environment variables from `.env` should override for API calls when using this wrapper. If behavior is confusing, run `firecrawl logout` once, then use only `.env` + wrapper.

## Security

- Never commit `.env`.
- If a key was pasted into chat or a log, rotate it in your Firecrawl dashboard.
