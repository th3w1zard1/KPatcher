# Prepends this scripts directory to the *user* PATH so `firecrawl` resolves to scripts\firecrawl.cmd
# before npm's global firecrawl. Run once; open a new terminal afterward.

$ErrorActionPreference = 'Stop'
$dir = $PSScriptRoot
$userPath = [Environment]::GetEnvironmentVariable('Path', 'User')
$parts = $userPath -split ';' | Where-Object { $_ }
$norm = { param($p) try { (Resolve-Path -LiteralPath $p).Path.TrimEnd('\') } catch { $null } }
$dirN = & $norm $dir
$already = $false
foreach ($p in $parts) {
    $n = & $norm $p
    if ($n -and $dirN -and $n -eq $dirN) { $already = $true; break }
}
if ($already) {
    Write-Host "PATH already contains: $dir"
    exit 0
}
$newPath = "$dir;$userPath"
[Environment]::SetEnvironmentVariable('Path', $newPath, 'User')
$env:Path = "$dir;$env:Path"
Write-Host "Prepended to user PATH: $dir"
Write-Host "Open a new terminal (or Cursor window) so every app picks up the change."
