# Appends a one-liner to your PowerShell profile that dot-sources Import-KPatcherFirecrawlEnv.ps1.
# Effect: any interactive PowerShell session loads FIRECRAWL_* from KPatcher .env even if you run
# the npm `firecrawl` directly (without going through scripts\firecrawl.cmd).

$ErrorActionPreference = 'Stop'
$importScript = Join-Path $PSScriptRoot 'Import-KPatcherFirecrawlEnv.ps1'
$importLine = @"
# KPatcher: Firecrawl credentials from repo .env (added by Install-FirecrawlProfileHook.ps1)
. `"$importScript`"
"@

if (-not (Test-Path -LiteralPath $PROFILE)) {
    New-Item -ItemType File -Path $PROFILE -Force | Out-Null
}
$existing = Get-Content -LiteralPath $PROFILE -Raw -ErrorAction SilentlyContinue
if ($existing -and $existing.Contains('Import-KPatcherFirecrawlEnv.ps1')) {
    Write-Host 'Profile already references Import-KPatcherFirecrawlEnv.ps1'
    exit 0
}
Add-Content -LiteralPath $PROFILE -Value "`n$importLine`n"
Write-Host "Updated profile: $PROFILE"
