# Wrapper: loads KPatcher .env then runs the *next* firecrawl on PATH (skips this scripts folder).
# Install: prepend this directory to your user PATH (see Add-FirecrawlEnvToUserPath.ps1).

$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot

. (Join-Path $here 'Import-KPatcherFirecrawlEnv.ps1')

if (-not $env:FIRECRAWL_API_KEY) {
    Write-Error 'FIRECRAWL_API_KEY is not set after loading .env. Check your .env file.'
    exit 1
}

function Get-NextFirecrawlOnPath {
    param([string] $SkipDirectory)
    $skip = (Resolve-Path -LiteralPath $SkipDirectory).Path.TrimEnd('\')
    $dirs = $env:Path -split ';' | Where-Object { $_ -and (Test-Path -LiteralPath $_) }
    foreach ($dir in $dirs) {
        $d = (Resolve-Path -LiteralPath $dir).Path.TrimEnd('\')
        if ($d -eq $skip) { continue }
        foreach ($name in @('firecrawl.cmd', 'firecrawl.ps1', 'firecrawl.bat', 'firecrawl.exe')) {
            $p = Join-Path $dir $name
            if (Test-Path -LiteralPath $p) { return $p }
        }
        $shim = Join-Path $dir 'firecrawl'
        if ((Test-Path -LiteralPath $shim) -and -not (Test-Path -LiteralPath $shim -PathType Container)) {
            return $shim
        }
    }
    $npmCmd = Join-Path $env:APPDATA 'npm\firecrawl.cmd'
    if (Test-Path -LiteralPath $npmCmd) { return $npmCmd }
    return $null
}

$real = Get-NextFirecrawlOnPath -SkipDirectory $here
if (-not $real) {
    Write-Error 'Could not find the real firecrawl CLI (npm global). Install with: npm install -g firecrawl-cli'
    exit 1
}

& $real @args
exit $LASTEXITCODE
