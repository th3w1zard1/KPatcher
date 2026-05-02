<#
.SYNOPSIS
    Projects supported extracted archives into anonymized committed fixtures.
#>
[CmdletBinding()]
param(
    [string]$BaseDir = (Join-Path $PSScriptRoot '..\tests\KPatcher.Tests\test_files\integration_tslpatcher_mods'),
    [string]$InventoryPath = (Join-Path $PSScriptRoot '..\tests\KPatcher.Tests\test_files\integration_tslpatcher_mods\inventory\inventory.json'),
    [string]$DestinationRoot = (Join-Path $PSScriptRoot '..\tests\KPatcher.Tests\test_files\integration_tslpatcher_mods\fixtures'),
    [switch]$CleanDestination
)

$ErrorActionPreference = 'Stop'

function Copy-DirectoryFiltered {
    param(
        [Parameter(Mandatory = $true)][string]$SourceDir,
        [Parameter(Mandatory = $true)][string]$DestinationDir
    )

    New-Item -ItemType Directory -Path $DestinationDir -Force | Out-Null

    foreach ($file in Get-ChildItem -LiteralPath $SourceDir -File -ErrorAction SilentlyContinue) {
        $lowerName = $file.Name.ToLowerInvariant()
        $lowerExt = $file.Extension.ToLowerInvariant()

        if ($lowerExt -eq '.exe') {
            continue
        }

        if (($lowerExt -in @('.txt', '.rtf', '.md', '.pdf', '.doc', '.docx')) -and ($lowerName -match 'readme|changelog|license|credits|install')) {
            continue
        }

        Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $DestinationDir $file.Name) -Force
    }

    foreach ($subDir in Get-ChildItem -LiteralPath $SourceDir -Directory -ErrorAction SilentlyContinue) {
        Copy-DirectoryFiltered -SourceDir $subDir.FullName -DestinationDir (Join-Path $DestinationDir $subDir.Name)
    }
}

function Invoke-TextScrub {
    param([Parameter(Mandatory = $true)][string]$Root)

    $textExtensions = @('.ini', '.cfg', '.json', '.yaml', '.yml', '.nss', '.txt', '.rtf', '.md')
    $replacements = @(
        @{ Pattern = '(?i)https?://(?:www\.)?deadlystream\.com\S*'; Replacement = 'https://example.invalid/mod-source' },
        @{ Pattern = '(?i)https?://(?:www\.)?dropbox\.com\S*'; Replacement = 'https://example.invalid/file-host' },
        @{ Pattern = '(?i)https?://(?:www\.)?github\.com\S*'; Replacement = 'https://example.invalid/code-host' },
        @{ Pattern = '(?i)deadlystream'; Replacement = 'project-source' },
        @{ Pattern = '(?i)dropbox'; Replacement = 'file-host' },
        @{ Pattern = '(?im)^(\s*(?:author|authors|mod author|mod authors|created by|made by|credits?)\s*[:=-]\s*).*$'; Replacement = '$1[redacted]' },
        @{ Pattern = '(?im)^(\s*(?:original mod(?:ification)? by|inspired by|ported by|requested by|special thanks(?: to)?|thanks to)\s*[:=-]\s*).*$'; Replacement = '$1[redacted]' },
        @{ Pattern = '(?im)^(\s*by\s+).*$'; Replacement = '$1[redacted]' }
    )

    foreach ($file in Get-ChildItem -Path $Root -Recurse -File -ErrorAction SilentlyContinue) {
        if ($textExtensions -notcontains $file.Extension.ToLowerInvariant()) {
            continue
        }

        $content = Get-Content -LiteralPath $file.FullName -Raw -ErrorAction SilentlyContinue
        if ($null -eq $content) {
            continue
        }

        $updated = $content
        foreach ($replacement in $replacements) {
            $updated = [regex]::Replace($updated, $replacement.Pattern, $replacement.Replacement)
        }

        if (-not $updated.Equals($content, [System.StringComparison]::Ordinal)) {
            Set-Content -LiteralPath $file.FullName -Value $updated -Encoding UTF8 -NoNewline
        }
    }
}

if (-not (Test-Path -LiteralPath $InventoryPath -PathType Leaf)) {
    throw "Inventory not found at '$InventoryPath'. Run BuildIntegrationTslpatcherModInventory.ps1 first."
}

New-Item -ItemType Directory -Path $DestinationRoot -Force | Out-Null

if ($CleanDestination) {
    Get-ChildItem -LiteralPath $DestinationRoot -Force |
        Where-Object { $_.Name -ne 'manifest.json' } |
        Remove-Item -Recurse -Force
}

$inventory = @(Get-Content -LiteralPath $InventoryPath -Raw | ConvertFrom-Json)
$manifestRows = New-Object System.Collections.Generic.List[object]

foreach ($row in $inventory | Where-Object { $_.Supported -and $_.TslpatchdataRelativePath }) {
    $sourceTsl = Join-Path $BaseDir ($row.TslpatchdataRelativePath -replace '/', [System.IO.Path]::DirectorySeparatorChar)
    if (-not (Test-Path -LiteralPath $sourceTsl -PathType Container)) {
        continue
    }

    $caseRoot = Join-Path $DestinationRoot $row.CaseId
    $caseTsl = Join-Path $caseRoot 'tslpatchdata'
    if (Test-Path -LiteralPath $caseRoot) {
        Remove-Item -LiteralPath $caseRoot -Recurse -Force
    }

    Copy-DirectoryFiltered -SourceDir $sourceTsl -DestinationDir $caseTsl
    Invoke-TextScrub -Root $caseRoot

    $manifestRows.Add([PSCustomObject]@{
            caseId = $row.CaseId
            sourceGame = $row.SourceGame
            sourceArchiveId = $row.ArchiveId
            sourceArchiveName = $row.ArchiveName
            sourceArchiveBaseName = $row.ArchiveBaseName
            sourceArchiveRelativePath = $row.RelativeArchivePath
            sourceExtractedRelativePath = $row.ExtractedRelativePath
            fixtureRelativePath = $row.CaseId
            tslpatchdataRelativePath = 'tslpatchdata'
            primaryEntryRelativePath = $row.PrimaryEntryRelativePath
            namespaceConfigRelativePath = $row.NamespaceConfigRelativePath
            hasNamespaces = [bool]$row.HasNamespaces
            includeInstallerSmoke = [bool]$row.IncludeInstallerSmoke
            installListKeyCount = [int]$row.InstallListKeyCount
            twoDAListKeyCount = [int]$row.TwoDAListKeyCount
            gffListKeyCount = [int]$row.GFFListKeyCount
            ssfListKeyCount = [int]$row.SSFListKeyCount
            ncsListKeyCount = [int]$row.NCSListKeyCount
            tlkListKeyCount = [int]$row.TLKListKeyCount
            compileListKeyCount = [int]$row.CompileListKeyCount
            hackListKeyCount = [int]$row.HackListKeyCount
        }) | Out-Null
}

$manifestPath = Join-Path $DestinationRoot 'manifest.json'
$manifestRows | Sort-Object caseId | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $manifestPath -Encoding UTF8

Write-Host "Projected fixtures: $($manifestRows.Count)"
Write-Host "Fixture manifest: $manifestPath"
