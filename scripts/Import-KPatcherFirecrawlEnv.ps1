# Dot-source this file to set FIRECRAWL_* (and other keys) from the KPatcher repo .env in the current process.
# Resolution order for the env file:
#   1) $env:FIRECRAWL_ENV_FILE (full path to any .env)
#   2) $env:KPATCHER_ROOT\.env
#   3) <this repo root>\.env  (repo root = parent of the scripts directory)

# Captured when this file is dot-sourced (do not rely on $PSScriptRoot inside nested functions).
$script:KPatcherFirecrawlEnvScriptsDir = $PSScriptRoot

function Get-KPatcherFirecrawlEnvFilePath {
    if ($env:FIRECRAWL_ENV_FILE) {
        return $env:FIRECRAWL_ENV_FILE.Trim()
    }
    if ($env:KPATCHER_ROOT) {
        return (Join-Path $env:KPATCHER_ROOT.TrimEnd('\', '/') '.env')
    }
    $repoRoot = Split-Path -Parent $script:KPatcherFirecrawlEnvScriptsDir
    return (Join-Path $repoRoot '.env')
}

function Import-KPatcherDotEnv {
    param(
        [Parameter(Mandatory)]
        [string] $EnvFilePath
    )
    if (-not (Test-Path -LiteralPath $EnvFilePath)) {
        Write-Error "Env file not found: $EnvFilePath. Set FIRECRAWL_ENV_FILE or KPATCHER_ROOT, or create .env at the KPatcher repo root."
        return $false
    }
    Get-Content -LiteralPath $EnvFilePath -Encoding UTF8 | ForEach-Object {
        $line = $_.Trim()
        if (-not $line -or $line.StartsWith('#')) { return }
        $eq = $line.IndexOf('=')
        if ($eq -lt 1) { return }
        $name = $line.Substring(0, $eq).Trim()
        if ($name -notmatch '^[A-Za-z_][A-Za-z0-9_]*$') { return }
        $val = $line.Substring($eq + 1).Trim()
        if (
            ($val.Length -ge 2 -and $val.StartsWith('"') -and $val.EndsWith('"')) -or
            ($val.Length -ge 2 -and $val.StartsWith("'") -and $val.EndsWith("'"))
        ) {
            $val = $val.Substring(1, $val.Length - 2)
        }
        Set-Item -Path "env:$name" -Value $val
    }
    return $true
}

$envFile = Get-KPatcherFirecrawlEnvFilePath
[void](Import-KPatcherDotEnv -EnvFilePath $envFile)
