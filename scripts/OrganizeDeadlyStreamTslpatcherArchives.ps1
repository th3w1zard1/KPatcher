<#
.SYNOPSIS
    Keep only DeadlyStream (and sibling) zips that contain a tslpatchdata path; sort into K1 vs TSL folders.

.DESCRIPTION
    Uses 7-Zip CLI only to list archive contents (7z l -ba). Deletes archives with no path containing
    "tslpatchdata" (case-insensitive). Moves or copies survivors into:
      integration_tslpatcher_mods/deadlystream_k1/
      integration_tslpatcher_mods/deadlystream_tsl/
    Classification uses file IDs from .firecrawl/k1-full-links.json vs k2-full-links.json.
    If a file ID appears on both build pages, the zip is copied to both folders.

.PARAMETER BaseDir
    integration_tslpatcher_mods root (contains deadlystream_downloaded).
.PARAMETER SevenZip
    Path to 7z.exe.
#>
[CmdletBinding()]
param(
    [string]$BaseDir = (Join-Path $PSScriptRoot '..\tests\KPatcher.Tests\test_files\integration_tslpatcher_mods'),
    [string]$LinksDir = (Join-Path $PSScriptRoot '..\.firecrawl'),
    [string]$SevenZip = 'C:\Program Files\7-Zip\7z.exe'
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $SevenZip)) {
    throw "7-Zip not found at $SevenZip. Install 7-Zip or pass -SevenZip."
}

function Get-DeadlyStreamIdsFromLinks {
    param([object]$Json)
    $ids = [System.Collections.Generic.HashSet[int]]::new()
    foreach ($link in $Json.links) {
        if ($link -match 'deadlystream\.com/files/file/(\d+)-') { [void]$ids.Add([int]$Matches[1]) }
    }
    $ids
}

$k1Path = Join-Path $LinksDir 'k1-full-links.json'
$k2Path = Join-Path $LinksDir 'k2-full-links.json'
if (-not (Test-Path $k1Path) -or -not (Test-Path $k2Path)) {
    throw "Missing $k1Path or $k2Path"
}

$k1Json = Get-Content $k1Path -Raw | ConvertFrom-Json
$k2Json = Get-Content $k2Path -Raw | ConvertFrom-Json
$k1Ids = Get-DeadlyStreamIdsFromLinks $k1Json
$k2Ids = Get-DeadlyStreamIdsFromLinks $k2Json

$srcDir = Join-Path $BaseDir 'deadlystream_downloaded'
$k1Out = Join-Path $BaseDir 'deadlystream_k1'
$tslOut = Join-Path $BaseDir 'deadlystream_tsl'
$unlisted = Join-Path $BaseDir 'deadlystream_unlisted'
foreach ($d in $k1Out, $tslOut, $unlisted) {
    if (-not (Test-Path $d)) { New-Item -ItemType Directory -Path $d -Force | Out-Null }
}

# Zips: deadlystream_downloaded + loose zips in BaseDir only (not scenario_* / subfolders)
$zips = @()
if (Test-Path $srcDir) {
    $zips += Get-ChildItem -LiteralPath $srcDir -Filter *.zip -File
}
$zips += Get-ChildItem -LiteralPath $BaseDir -Filter *.zip -File

$removed = 0
$movedK1 = 0
$movedTsl = 0
$copiedBoth = 0
$unlistedCount = 0

foreach ($zip in $zips) {
    $listOutput = & $SevenZip l -ba $zip.FullName 2>&1 | Out-String
    $exit = $LASTEXITCODE
    $hasTslpatchdata = $listOutput -match 'tslpatchdata'

    if ($exit -ne 0 -or -not $hasTslpatchdata) {
        Write-Host "DELETE (no tslpatchdata or not archive): $($zip.Name)"
        Remove-Item -LiteralPath $zip.FullName -Force
        $removed++
        continue
    }

    if ($zip.BaseName -notmatch '^(\d+)-') {
        Write-Host "SKIP classify (no id- prefix): $($zip.Name) — moving to unlisted"
        $dest = Join-Path $unlisted $zip.Name
        if ($zip.DirectoryName -ne $unlisted) {
            Move-Item -LiteralPath $zip.FullName -Destination $dest -Force
            $unlistedCount++
        }
        continue
    }

    $id = [int]$Matches[1]
    $inK1 = $k1Ids.Contains($id)
    $inTsl = $k2Ids.Contains($id)

    if (-not $inK1 -and -not $inTsl) {
        Write-Host "UNLISTED ID $id : $($zip.Name)"
        $dest = Join-Path $unlisted $zip.Name
        Move-Item -LiteralPath $zip.FullName -Destination $dest -Force
        $unlistedCount++
        continue
    }

    if ($inK1 -and $inTsl) {
        Copy-Item -LiteralPath $zip.FullName -Destination (Join-Path $k1Out $zip.Name) -Force
        Copy-Item -LiteralPath $zip.FullName -Destination (Join-Path $tslOut $zip.Name) -Force
        Remove-Item -LiteralPath $zip.FullName -Force
        $copiedBoth++
        Write-Host "BOTH: $($zip.Name)"
        continue
    }

    if ($inK1) {
        Move-Item -LiteralPath $zip.FullName -Destination (Join-Path $k1Out $zip.Name) -Force
        $movedK1++
        Write-Host "K1: $($zip.Name)"
    } else {
        Move-Item -LiteralPath $zip.FullName -Destination (Join-Path $tslOut $zip.Name) -Force
        $movedTsl++
        Write-Host "TSL: $($zip.Name)"
    }
}

# Remove empty source folder if applicable
if ((Test-Path $srcDir) -and -not (Get-ChildItem -LiteralPath $srcDir -Force -ErrorAction SilentlyContinue)) {
    Remove-Item -LiteralPath $srcDir -Force
    Write-Host "Removed empty deadlystream_downloaded"
}

Write-Host ""
Write-Host "Done. Removed: $removed | K1: $movedK1 | TSL: $movedTsl | Both copies: $copiedBoth | Unlisted: $unlistedCount"
