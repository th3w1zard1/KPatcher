<#
.SYNOPSIS
    Regenerates authoritative KOTOR Community Portal link manifests used by the real-mod pipeline.

.DESCRIPTION
    Uses the repository Firecrawl wrapper to scrape the K1 and K2 full build pages, writing raw link
    manifests compatible with the existing downloader and an additional normalized combined source catalog.
#>
[CmdletBinding()]
param(
    [string]$LinksDir = (Join-Path $PSScriptRoot '..\.firecrawl'),
    [string]$FirecrawlScript = (Join-Path $PSScriptRoot 'firecrawl.ps1'),
    [string]$K1Url = 'https://kotor.neocities.org/modding/mod_builds/k1/full',
    [string]$K2Url = 'https://kotor.neocities.org/modding/mod_builds/k2/full'
)

$ErrorActionPreference = 'Stop'

function ConvertTo-CanonicalUrl {
    param([Parameter(Mandatory = $true)][string]$Url)

    $trimmed = $Url.Trim()
    if ([string]::IsNullOrWhiteSpace($trimmed)) {
        return $trimmed
    }

    if ($trimmed -match '^http://deadlystream\.com/') {
        $trimmed = $trimmed -replace '^http:', 'https:'
    }

    try {
        $builder = New-Object System.UriBuilder($trimmed)
        $builder.Fragment = ''
        $canonical = $builder.Uri.AbsoluteUri
    }
    catch {
        return $trimmed
    }

    if ($canonical.EndsWith('/')) {
        $uri = New-Object System.Uri($canonical)
        if ($uri.AbsolutePath -ne '/') {
            $canonical = $canonical.TrimEnd('/')
        }
    }

    return $canonical
}

function Invoke-FirecrawlLinksScrape {
    param(
        [Parameter(Mandatory = $true)][string]$Url,
        [Parameter(Mandatory = $true)][string]$OutputPath
    )

    & $FirecrawlScript scrape $Url --format links --json --pretty -o $OutputPath
    if ($LASTEXITCODE -ne 0) {
        throw "Firecrawl scrape failed for '$Url' with exit code $LASTEXITCODE."
    }
}

function Get-LinkRecords {
    param(
        [Parameter(Mandatory = $true)][object]$Manifest,
        [Parameter(Mandatory = $true)][string]$SourceGame,
        [Parameter(Mandatory = $true)][string]$PortalUrl
    )

    $records = New-Object System.Collections.Generic.List[object]
    $seen = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)

    foreach ($link in @($Manifest.links)) {
        if ([string]::IsNullOrWhiteSpace($link)) {
            continue
        }

        $canonical = ConvertTo-CanonicalUrl -Url $link
        if (-not $seen.Add($canonical)) {
            continue
        }

        $hostName = ''
        try {
            $hostName = ([System.Uri]$canonical).Host
        }
        catch {
        }

        $archiveId = $null
        $slug = $null
        if ($canonical -match 'deadlystream\.com/files/file/(\d+)-([^/?#]+)') {
            $archiveId = [int]$Matches[1]
            $slug = $Matches[2]
        }

        $records.Add([PSCustomObject]@{
                sourceGame = $SourceGame
                portalUrl = $PortalUrl
                originalUrl = $link
                canonicalUrl = $canonical
                host = $hostName
                isDeadlyStreamFilePage = ($null -ne $archiveId)
                archiveId = $archiveId
                slug = $slug
            }) | Out-Null
    }

    return $records
}

if (-not (Test-Path -LiteralPath $FirecrawlScript)) {
    throw "Firecrawl wrapper not found at '$FirecrawlScript'."
}

New-Item -ItemType Directory -Path $LinksDir -Force | Out-Null

$k1Path = Join-Path $LinksDir 'k1-full-links.json'
$k2Path = Join-Path $LinksDir 'k2-full-links.json'
$catalogPath = Join-Path $LinksDir 'portal-source-records.json'

Invoke-FirecrawlLinksScrape -Url $K1Url -OutputPath $k1Path
Invoke-FirecrawlLinksScrape -Url $K2Url -OutputPath $k2Path

$k1Manifest = Get-Content -LiteralPath $k1Path -Raw | ConvertFrom-Json
$k2Manifest = Get-Content -LiteralPath $k2Path -Raw | ConvertFrom-Json

$records = New-Object System.Collections.Generic.List[object]
foreach ($record in Get-LinkRecords -Manifest $k1Manifest -SourceGame 'K1' -PortalUrl $K1Url) {
    $records.Add($record) | Out-Null
}
foreach ($record in Get-LinkRecords -Manifest $k2Manifest -SourceGame 'TSL' -PortalUrl $K2Url) {
    $records.Add($record) | Out-Null
}

$sorted = $records | Sort-Object sourceGame, host, archiveId, canonicalUrl
[PSCustomObject]@{
    generatedAtUtc = [DateTime]::UtcNow.ToString('o')
    sources = $sorted
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $catalogPath -Encoding UTF8

Write-Host "Regenerated manifests:"
Write-Host "  $k1Path"
Write-Host "  $k2Path"
Write-Host "  $catalogPath"
Write-Host "K1 links: $(@($k1Manifest.links).Count)"
Write-Host "TSL links: $(@($k2Manifest.links).Count)"
Write-Host "Catalog records: $(@($sorted).Count)"
