<#
.SYNOPSIS
    Extracts downloaded mod archives with 7-Zip and builds a machine-readable inventory.

.DESCRIPTION
    Scans the maintainer cache under tests/KPatcher.Tests/test_files/integration_tslpatcher_mods,
    extracts archives into an extracted cache, identifies installer roots, assigns opaque case IDs,
    and writes combined supported/quarantine manifests for later projection and test generation.
#>
[CmdletBinding()]
param(
    [string]$BaseDir = (Join-Path $PSScriptRoot '..\tests\KPatcher.Tests\test_files\integration_tslpatcher_mods'),
    [string]$SevenZip,
    [string]$ExtractedDir,
    [string]$InventoryDir,
    [switch]$ForceReextract
)

$ErrorActionPreference = 'Stop'

if (-not $ExtractedDir) {
    $ExtractedDir = Join-Path $BaseDir 'extracted'
}
if (-not $InventoryDir) {
    $InventoryDir = Join-Path $BaseDir 'inventory'
}

function Resolve-SevenZipPath {
    param([string]$RequestedPath)

    if ($RequestedPath) {
        if (Test-Path -LiteralPath $RequestedPath) {
            return $RequestedPath
        }

        throw "7-Zip not found at '$RequestedPath'."
    }

    $command = Get-Command 7z -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $defaultPath = 'C:\Program Files\7-Zip\7z.exe'
    if (Test-Path -LiteralPath $defaultPath) {
        return $defaultPath
    }

    throw "7-Zip not found on PATH and not found at '$defaultPath'."
}

$SevenZip = Resolve-SevenZipPath -RequestedPath $SevenZip

New-Item -ItemType Directory -Path $ExtractedDir -Force | Out-Null
New-Item -ItemType Directory -Path $InventoryDir -Force | Out-Null

function ConvertTo-NormalizedRelativePath {
    param([Parameter(Mandatory = $true)][string]$PathValue)

    $normalized = $PathValue.Replace('\', '/')
    while ($normalized.StartsWith('./', [System.StringComparison]::Ordinal)) {
        $normalized = $normalized.Substring(2)
    }

    return $normalized.Trim('/')
}

function Get-ArchiveId {
    param([Parameter(Mandatory = $true)][string]$ArchiveName)

    if ($ArchiveName -match '^(\d+)-') {
        return [int]$Matches[1]
    }

    return $null
}

function Get-GameKey {
    param([Parameter(Mandatory = $true)][string]$FolderName)

    switch -Regex ($FolderName) {
        '^deadlystream_k1$' { return 'K1' }
        '^deadlystream_tsl$' { return 'TSL' }
        '^deadlystream_cross$' { return 'Cross' }
        default { return 'Unknown' }
    }
}

function Invoke-ArchiveExtraction {
    param(
        [Parameter(Mandatory = $true)][string]$ArchivePath,
        [Parameter(Mandatory = $true)][string]$DestinationPath
    )

    if (Test-Path -LiteralPath $DestinationPath) {
        Remove-Item -LiteralPath $DestinationPath -Recurse -Force
    }

    New-Item -ItemType Directory -Path $DestinationPath -Force | Out-Null
    $output = & $SevenZip x $ArchivePath "-o$DestinationPath" -y 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        throw $output
    }

    return $output
}

function Get-TslpatchdataDirectories {
    param([Parameter(Mandatory = $true)][string]$Root)

    Get-ChildItem -Path $Root -Recurse -Directory -ErrorAction SilentlyContinue |
    Where-Object { $_.Name.Equals('tslpatchdata', [System.StringComparison]::OrdinalIgnoreCase) } |
    Where-Object { $_.FullName -notmatch '(^|[\\/])__MACOSX([\\/]|$)' } |
        Sort-Object @{ Expression = { $_.FullName.Length } }, @{ Expression = { $_.FullName } }
}

function Get-IniSectionCounts {
    param([Parameter(Mandatory = $true)][string]$IniPath)

    $counts = [ordered]@{
        InstallList = 0
        TwoDAList = 0
        GFFList = 0
        SSFList = 0
        NCSList = 0
        TLKList = 0
        CompileList = 0
        HACKList = 0
    }

    $sectionKeys = @{}
    foreach ($name in $counts.Keys) {
        $sectionKeys[$name] = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
    }

    $section = $null
    foreach ($line in Get-Content -LiteralPath $IniPath -Encoding UTF8) {
        $trimmed = $line.Trim()
        if (-not $trimmed -or $trimmed.StartsWith(';') -or $trimmed.StartsWith('#')) {
            continue
        }

        if ($trimmed -match '^\[(.+)\]$') {
            $section = $Matches[1].Trim()
            continue
        }

        if ($trimmed -notmatch '=') {
            continue
        }

        $keyName = $trimmed.Split('=', 2)[0].Trim()

        switch -Regex ($section) {
            '^InstallList$' { $null = $sectionKeys.InstallList.Add($keyName); continue }
            '^2DAList$' { $null = $sectionKeys.TwoDAList.Add($keyName); continue }
            '^GFFList$' { $null = $sectionKeys.GFFList.Add($keyName); continue }
            '^SSFList$' { $null = $sectionKeys.SSFList.Add($keyName); continue }
            '^NCSList$' { $null = $sectionKeys.NCSList.Add($keyName); continue }
            '^TLKList$' { $null = $sectionKeys.TLKList.Add($keyName); continue }
            '^CompileList$' { $null = $sectionKeys.CompileList.Add($keyName); continue }
            '^HACKList$' { $null = $sectionKeys.HACKList.Add($keyName); continue }
        }
    }

    foreach ($name in @($counts.Keys)) {
        $counts[$name] = $sectionKeys[$name].Count
    }

    return $counts
}

function Get-PrimaryEntryFromNamespaces {
    param([Parameter(Mandatory = $true)][string]$NamespacePath)

    $sections = @{}
    $currentSection = $null
    foreach ($line in Get-Content -LiteralPath $NamespacePath -Encoding UTF8) {
        $trimmed = $line.Trim()
        if (-not $trimmed -or $trimmed.StartsWith(';') -or $trimmed.StartsWith('#')) {
            continue
        }

        if ($trimmed -match '^\[(.+)\]$') {
            $currentSection = $Matches[1].Trim()
            if (-not $sections.ContainsKey($currentSection)) {
                $sections[$currentSection] = @{}
            }
            continue
        }

        if (-not $currentSection -or $trimmed -notmatch '=') {
            continue
        }

        $parts = $trimmed.Split('=', 2)
        $sections[$currentSection][$parts[0].Trim()] = $parts[1].Trim()
    }

    $namespacesSection = $null
    foreach ($key in $sections.Keys) {
        if ($key.Equals('Namespaces', [System.StringComparison]::OrdinalIgnoreCase)) {
            $namespacesSection = $sections[$key]
            break
        }
    }

    if ($null -eq $namespacesSection -or $namespacesSection.Count -eq 0) {
        return $null
    }

    $firstNamespaceId = $namespacesSection.GetEnumerator() | Sort-Object Name | Select-Object -First 1
    if ($null -eq $firstNamespaceId) {
        return $null
    }

    $namespaceSectionName = $firstNamespaceId.Value
    $namespaceSection = $null
    foreach ($key in $sections.Keys) {
        if ($key.Equals($namespaceSectionName, [System.StringComparison]::OrdinalIgnoreCase)) {
            $namespaceSection = $sections[$key]
            break
        }
    }

    if ($null -eq $namespaceSection -or -not $namespaceSection.ContainsKey('IniName')) {
        return $null
    }

    $iniName = $namespaceSection['IniName']
    $dataPath = ''
    if ($namespaceSection.ContainsKey('DataPath')) {
        $dataPath = $namespaceSection['DataPath']
    }

    if ([string]::IsNullOrWhiteSpace($dataPath) -or $dataPath -eq '.') {
        return ConvertTo-NormalizedRelativePath -PathValue $iniName
    }

    return ConvertTo-NormalizedRelativePath -PathValue (Join-Path $dataPath $iniName)
}

function Get-PrimaryEntryRelativePath {
    param([Parameter(Mandatory = $true)][string]$TslpatchdataPath)

    foreach ($name in @('changes.ini', 'changes.yaml', 'changes.yml')) {
        $candidate = Join-Path $TslpatchdataPath $name
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            return ConvertTo-NormalizedRelativePath -PathValue ([System.IO.Path]::GetRelativePath($TslpatchdataPath, $candidate))
        }
    }

    $files = Get-ChildItem -Path $TslpatchdataPath -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object {
            $_.Extension.Equals('.ini', [System.StringComparison]::OrdinalIgnoreCase) -or
            $_.Extension.Equals('.yaml', [System.StringComparison]::OrdinalIgnoreCase) -or
            $_.Extension.Equals('.yml', [System.StringComparison]::OrdinalIgnoreCase)
        } |
        Where-Object { -not $_.Name.Equals('namespaces.ini', [System.StringComparison]::OrdinalIgnoreCase) } |
        Sort-Object FullName

    if ($files.Count -eq 0) {
        return $null
    }

    return ConvertTo-NormalizedRelativePath -PathValue ([System.IO.Path]::GetRelativePath($TslpatchdataPath, $files[0].FullName))
}

function Write-JsonArrayFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][object[]]$Items
    )

    $json = if ($Items.Count -eq 0) {
        '[]'
    }
    else {
        $Items | ConvertTo-Json -Depth 8
    }

    Set-Content -LiteralPath $Path -Value $json -Encoding UTF8
}

$archives = New-Object System.Collections.Generic.List[object]
foreach ($folder in @('deadlystream_k1', 'deadlystream_tsl', 'deadlystream_cross', 'deadlystream_unlisted')) {
    $path = Join-Path $BaseDir $folder
    if (-not (Test-Path -LiteralPath $path)) {
        continue
    }

    foreach ($zip in Get-ChildItem -LiteralPath $path -Filter *.zip -File | Sort-Object Name) {
        $archives.Add([PSCustomObject]@{
                FolderName = $folder
                GameKey = Get-GameKey -FolderName $folder
                File = $zip
            }) | Out-Null
    }
}

$rows = New-Object System.Collections.Generic.List[object]

foreach ($archive in $archives) {
    $archiveFile = $archive.File
    $archiveId = Get-ArchiveId -ArchiveName $archiveFile.Name
    $archiveBaseName = $archiveFile.BaseName
    $extractRoot = Join-Path (Join-Path $ExtractedDir $archive.GameKey.ToLowerInvariant()) $archiveFile.BaseName
    $extractLog = ''
    $quarantineReason = $null

    try {
        if ($ForceReextract -or -not (Test-Path -LiteralPath $extractRoot)) {
            $extractLog = Invoke-ArchiveExtraction -ArchivePath $archiveFile.FullName -DestinationPath $extractRoot
        }
        else {
            $extractLog = 'Reused existing extracted tree.'
        }
    }
    catch {
        $quarantineReason = 'ExtractionFailed'
        $extractLog = $_.Exception.Message
    }

    $tslDirs = @()
    if (-not $quarantineReason -and (Test-Path -LiteralPath $extractRoot)) {
        $tslDirs = @(Get-TslpatchdataDirectories -Root $extractRoot)
        if ($tslDirs.Count -eq 0) {
            $quarantineReason = 'MissingTslpatchdata'
        }
    }

    if ($quarantineReason -or $tslDirs.Count -eq 0) {
        $rows.Add([PSCustomObject]@{
                ArchiveId = $archiveId
                ArchiveName = $archiveFile.Name
                ArchiveBaseName = $archiveBaseName
                SourceGame = $archive.GameKey
                SourceFolder = $archive.FolderName
                RelativeArchivePath = ConvertTo-NormalizedRelativePath -PathValue ([System.IO.Path]::GetRelativePath($BaseDir, $archiveFile.FullName))
                ExtractedRelativePath = ConvertTo-NormalizedRelativePath -PathValue ([System.IO.Path]::GetRelativePath($BaseDir, $extractRoot))
                TslpatchdataRelativePath = $null
                PrimaryEntryRelativePath = $null
                NamespaceConfigRelativePath = $null
                HasNamespaces = $false
                InstallListKeyCount = 0
                TwoDAListKeyCount = 0
                GFFListKeyCount = 0
                SSFListKeyCount = 0
                NCSListKeyCount = 0
                TLKListKeyCount = 0
                CompileListKeyCount = 0
                HackListKeyCount = 0
                IncludeInstallerSmoke = $false
                Supported = $false
                QuarantineReason = $quarantineReason
                ExtractionLog = $extractLog
            }) | Out-Null
        continue
    }

    foreach ($tslDir in $tslDirs) {
        $selectedTsl = $tslDir.FullName
        $entryQuarantineReason = $null
        $primaryEntry = Get-PrimaryEntryRelativePath -TslpatchdataPath $selectedTsl
        $namespaceConfig = $null
        $iniSectionCounts = [ordered]@{
            InstallList = 0
            TwoDAList = 0
            GFFList = 0
            SSFList = 0
            NCSList = 0
            TLKList = 0
            CompileList = 0
            HACKList = 0
        }

        $namespacePath = Join-Path $selectedTsl 'namespaces.ini'
        if (Test-Path -LiteralPath $namespacePath) {
            $namespaceConfig = 'namespaces.ini'
            if (-not $primaryEntry) {
                $primaryEntry = Get-PrimaryEntryFromNamespaces -NamespacePath $namespacePath
            }
        }

        if (-not $primaryEntry) {
            $entryQuarantineReason = 'MissingPrimaryEntry'
        }

        $primaryPath = if ($primaryEntry) { Join-Path $selectedTsl ($primaryEntry.Replace('/', [System.IO.Path]::DirectorySeparatorChar)) } else { $null }
        if ($primaryPath -and (Test-Path -LiteralPath $primaryPath) -and [System.IO.Path]::GetExtension($primaryPath).Equals('.ini', [System.StringComparison]::OrdinalIgnoreCase)) {
            $iniSectionCounts = Get-IniSectionCounts -IniPath $primaryPath
        }

        $rows.Add([PSCustomObject]@{
                ArchiveId = $archiveId
                ArchiveName = $archiveFile.Name
                ArchiveBaseName = $archiveBaseName
                SourceGame = $archive.GameKey
                SourceFolder = $archive.FolderName
                RelativeArchivePath = ConvertTo-NormalizedRelativePath -PathValue ([System.IO.Path]::GetRelativePath($BaseDir, $archiveFile.FullName))
                ExtractedRelativePath = ConvertTo-NormalizedRelativePath -PathValue ([System.IO.Path]::GetRelativePath($BaseDir, $extractRoot))
                TslpatchdataRelativePath = ConvertTo-NormalizedRelativePath -PathValue ([System.IO.Path]::GetRelativePath($BaseDir, $selectedTsl))
                PrimaryEntryRelativePath = $primaryEntry
                NamespaceConfigRelativePath = $namespaceConfig
                HasNamespaces = ($null -ne $namespaceConfig)
                InstallListKeyCount = [int]$iniSectionCounts.InstallList
                TwoDAListKeyCount = [int]$iniSectionCounts.TwoDAList
                GFFListKeyCount = [int]$iniSectionCounts.GFFList
                SSFListKeyCount = [int]$iniSectionCounts.SSFList
                NCSListKeyCount = [int]$iniSectionCounts.NCSList
                TLKListKeyCount = [int]$iniSectionCounts.TLKList
                CompileListKeyCount = [int]$iniSectionCounts.CompileList
                HackListKeyCount = [int]$iniSectionCounts.HACKList
                IncludeInstallerSmoke = (-not $entryQuarantineReason -and $iniSectionCounts.InstallList -gt 0 -and $iniSectionCounts.TwoDAList -eq 0 -and $iniSectionCounts.GFFList -eq 0 -and $iniSectionCounts.SSFList -eq 0 -and $iniSectionCounts.NCSList -eq 0 -and $iniSectionCounts.TLKList -eq 0 -and $iniSectionCounts.CompileList -eq 0 -and $iniSectionCounts.HACKList -eq 0)
                Supported = (-not $entryQuarantineReason)
                QuarantineReason = $entryQuarantineReason
                ExtractionLog = $extractLog
            }) | Out-Null
    }
}

$k1Index = 0
$tslIndex = 0
$otherIndex = 0
foreach ($row in $rows | Sort-Object SourceGame, ArchiveId, ArchiveName, TslpatchdataRelativePath) {
    switch ($row.SourceGame) {
        'K1' {
            $row | Add-Member -NotePropertyName CaseId -NotePropertyValue ('k1_p{0:D3}' -f $k1Index) -Force
            $k1Index++
        }
        'TSL' {
            $row | Add-Member -NotePropertyName CaseId -NotePropertyValue ('tsl_p{0:D3}' -f $tslIndex) -Force
            $tslIndex++
        }
        default {
            $row | Add-Member -NotePropertyName CaseId -NotePropertyValue ('other_p{0:D3}' -f $otherIndex) -Force
            $otherIndex++
        }
    }
}

$supported = @($rows | Where-Object { $_.Supported } | Sort-Object CaseId)
$quarantine = @($rows | Where-Object { -not $_.Supported } | Sort-Object ArchiveName)

$inventoryPath = Join-Path $InventoryDir 'inventory.json'
$quarantinePath = Join-Path $InventoryDir 'quarantine.json'
Write-JsonArrayFile -Path $inventoryPath -Items $supported
Write-JsonArrayFile -Path $quarantinePath -Items $quarantine

Write-Host "Inventory written: $inventoryPath"
Write-Host "Quarantine written: $quarantinePath"
Write-Host "Supported archives: $($supported.Count)"
Write-Host "Quarantined archives: $($quarantine.Count)"
