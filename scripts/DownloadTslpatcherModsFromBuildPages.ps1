<#
.SYNOPSIS
    Downloads TSLPatcher/HoloPatcher mods from the KOTOR Community Portal mod build pages.

.DESCRIPTION
    Reads link data from .firecrawl/k1-full-links.json and k2-full-links.json.
    DeadlyStream: Uses session cookie + CSRF from each file page; no login required.
    Dropbox: dl=1 forces direct download.

.PARAMETER OutDir
    Target directory for downloaded mods.
.PARAMETER LinksDir
    Directory containing k1-full-links.json and k2-full-links.json.
.PARAMETER DelaySeconds
    Seconds between DeadlyStream requests (default 1.5) to avoid throttling.
#>
[CmdletBinding()]
param(
    [string]$OutDir = (Join-Path $PSScriptRoot '..\tests\KPatcher.Tests\test_files\integration_tslpatcher_mods'),
    [string]$LinksDir = (Join-Path $PSScriptRoot '..\.firecrawl'),
    [double]$DelaySeconds = 1.5
)

$ErrorActionPreference = 'Stop'

$k1Path = Join-Path $LinksDir 'k1-full-links.json'
$k2Path = Join-Path $LinksDir 'k2-full-links.json'

if (-not (Test-Path $k1Path) -or -not (Test-Path $k2Path)) {
    Write-Warning "Link JSON files not found. Run Firecrawl scrape first."
    exit 1
}

$k1 = Get-Content $k1Path -Raw | ConvertFrom-Json
$k2 = Get-Content $k2Path -Raw | ConvertFrom-Json
$allLinks = @($k1.links) + @($k2.links) | Sort-Object -Unique

$deadlyStream = $allLinks | Where-Object { $_ -match 'deadlystream\.com/files/file/\d+' } | ForEach-Object {
    $u = if ($_ -match '^http:') { $_ -replace '^http:', 'https:' } else { $_ }
    $u -replace '\?.*$',''
} | Sort-Object -Unique

$dropbox = $allLinks | Where-Object { $_ -match 'dropbox\.com/s/' } | Sort-Object -Unique

if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir -Force | Out-Null }
$deadlyDir = Join-Path $OutDir 'deadlystream_downloaded'
if (-not (Test-Path $deadlyDir)) { New-Item -ItemType Directory -Path $deadlyDir -Force | Out-Null }

function Get-DeadlyStreamFileParts {
    param([string]$Url)
    $trim = $Url.TrimEnd('/')
    if ($trim -match '/files/file/(\d+)-(.+)$') {
        $id = $Matches[1]
        $slug = $Matches[2] -replace '[^\w\-]','_' -replace '_+','_'
        if (-not $slug) { $slug = 'file' }
        return @{ Id = [int]$id; Slug = $slug; ZipName = "${id}-${slug}.zip" }
    }
    return $null
}

function Get-SafeFilename {
    param([string]$Url)
    $p = Get-DeadlyStreamFileParts $Url
    if ($p) { return $p.ZipName }
    $slug = ([uri]$Url).Segments[-1] -replace '[^\w\-]','_' -replace '_+','_'
    if (-not $slug) { $slug = "mod_unknown" }
    "$slug.zip"
}

function Invoke-DeadlyStreamDownload {
    param([string]$FileUrl, [string]$OutPath, [Microsoft.PowerShell.Commands.WebRequestSession]$Session)
    $folder = Split-Path $OutPath
    if (-not (Test-Path $folder)) { New-Item -ItemType Directory -Path $folder -Force | Out-Null }
    $r1 = Invoke-WebRequest -Uri $FileUrl -WebSession $Session -UseBasicParsing -MaximumRedirection 5
    if ($r1.Content -match 'do=download&amp;csrfKey=([a-f0-9]+)') { $csrf = $Matches[1] }
    elseif ($r1.Content -match 'csrfKey["\s=]+([a-f0-9]+)') { $csrf = $Matches[1] }
    else { throw "No csrfKey in page" }
    $base = $FileUrl.TrimEnd('/')
    $dlUrl = "$base/?do=download&csrfKey=$csrf"
    Invoke-WebRequest -Uri $dlUrl -WebSession $Session -OutFile $OutPath -UseBasicParsing -MaximumRedirection 10
    $len = (Get-Item $OutPath).Length
    if ($len -lt 500) {
        $head = [System.IO.File]::ReadAllBytes($OutPath)
        $isZip = ($head.Length -ge 4 -and $head[0] -eq 0x50 -and $head[1] -eq 0x4B)
        if (-not $isZip) {
            $preview = [System.IO.File]::ReadAllText($OutPath) -replace '\s+',' '
            if ($preview -match '<(html|\!DOCTYPE)') { throw "Got HTML instead of file" }
        }
    }
    $len
}

# --- Dropbox ---
$dropboxOk = 0
foreach ($url in $dropbox) {
    $directUrl = if ($url -match '\?dl=0') { $url -replace '\?dl=0', '?dl=1' } else { $url + '?dl=1' }
    $filename = [System.IO.Path]::GetFileName(([Uri]$url).LocalPath)
    if (-not $filename -or $filename -eq '/') { $filename = "dropbox_$dropboxOk.zip" }
    $outPath = Join-Path $OutDir $filename
    try {
        Invoke-WebRequest -Uri $directUrl -OutFile $outPath -MaximumRedirection 5 -UseBasicParsing
        if ((Get-Item $outPath).Length -gt 100) { Write-Host "Dropbox OK: $filename"; $dropboxOk++ }
        else { Remove-Item $outPath -Force -ErrorAction SilentlyContinue }
    } catch { Write-Warning "Dropbox failed: $url" }
}

# --- DeadlyStream ---
# Migrate legacy slug-only archives (pre id-slug naming) to lowest file ID per slug.
$slugToUrls = @{}
foreach ($url in $deadlyStream) {
    $p = Get-DeadlyStreamFileParts $url
    if (-not $p) { continue }
    if (-not $slugToUrls[$p.Slug]) { $slugToUrls[$p.Slug] = @() }
    $slugToUrls[$p.Slug] += @{ Id = $p.Id; Url = $url }
}
foreach ($slug in $slugToUrls.Keys) {
    $legacyPath = Join-Path $deadlyDir "$slug.zip"
    if (-not (Test-Path $legacyPath)) { continue }
    $first = $slugToUrls[$slug] | Sort-Object Id | Select-Object -First 1
    $newPath = Join-Path $deadlyDir "$(($first.Id))-$slug.zip"
    if (-not (Test-Path $newPath)) {
        Move-Item -LiteralPath $legacyPath -Destination $newPath
        Write-Host "Migrated legacy ${slug}.zip -> $(Split-Path $newPath -Leaf)"
    }
}

$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$dsOk = 0
$dsFail = 0
$total = $deadlyStream.Count
$i = 0
foreach ($url in $deadlyStream) {
    $i++
    $filename = Get-SafeFilename $url
    $outPath = Join-Path $deadlyDir $filename
    if (Test-Path $outPath) {
        $existing = (Get-Item $outPath).Length
        if ($existing -gt 200) { Write-Host "[$i/$total] Skip (exists): $filename"; $dsOk++; continue }
    }
    Write-Host "[$i/$total] Downloading: $url"
    try {
        $bytes = Invoke-DeadlyStreamDownload -FileUrl $url -OutPath $outPath -Session $session
        Write-Host "  OK: $filename ($bytes bytes)"
        $dsOk++
    } catch {
        Write-Warning "  FAIL: $($_.Exception.Message)"
        if (Test-Path $outPath) { Remove-Item $outPath -Force -ErrorAction SilentlyContinue }
        $dsFail++
    }
    if ($DelaySeconds -gt 0 -and $i -lt $total) { Start-Sleep -Seconds $DelaySeconds }
}

Write-Host ""
Write-Host "Done. DeadlyStream: $dsOk OK, $dsFail failed. Dropbox: $dropboxOk."
