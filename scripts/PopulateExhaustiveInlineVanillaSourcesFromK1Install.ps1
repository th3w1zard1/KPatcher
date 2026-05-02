# Maintainer helper: extract vanilla-only GFF resources referenced by exhaustive fixture INIs
# from a local KotOR I bootstrap tree into the payload directory next to the selected changes.ini.
param(
    [Parameter(Mandatory = $true)]
    [string] $K1GameRoot,

    [Parameter(Mandatory = $true)]
    [string[]] $IniPaths,

    [switch] $OverwriteExisting
)

$ErrorActionPreference = 'Stop'

function Resolve-K1ModulesPath {
    param([Parameter(Mandatory = $true)][string] $GameRoot)

    foreach ($relative in @('modules', 'Modules')) {
        $candidate = Join-Path $GameRoot $relative
        if (Test-Path -LiteralPath $candidate -PathType Container) {
            return $candidate
        }
    }

    throw "K1 modules folder not found under bootstrap root: $GameRoot"
}

function Get-CandidateCapsulePaths {
    param(
        [Parameter(Mandatory = $true)][string] $OutputDirectory,
        [Parameter(Mandatory = $true)][string] $ModuleName,
        [Parameter(Mandatory = $true)][string] $K1ModulesRoot
    )

    $candidates = New-Object System.Collections.Generic.List[string]
    $localCandidate = Join-Path $OutputDirectory $ModuleName
    if (Test-Path -LiteralPath $localCandidate -PathType Leaf) {
        $candidates.Add($localCandidate)
    }

    $moduleBaseName = [System.IO.Path]::GetFileNameWithoutExtension($ModuleName)
    foreach ($candidateName in @(
        $ModuleName,
        ($moduleBaseName + '.rim'),
        ($moduleBaseName + '_s.rim')
    )) {
        $candidatePath = Join-Path $K1ModulesRoot $candidateName
        if ((Test-Path -LiteralPath $candidatePath -PathType Leaf) -and -not $candidates.Contains($candidatePath)) {
            $candidates.Add($candidatePath)
        }
    }

    return $candidates
}

function Ensure-KPatcherCoreLoaded {
    foreach ($assembly in [AppDomain]::CurrentDomain.GetAssemblies()) {
        if ($assembly.FullName -like 'KPatcher.Core,*') {
            return
        }
    }

    $repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
    $candidates = @(
        (Join-Path $repoRoot 'tests\KPatcher.Tests\bin\Debug\net9.0\KPatcher.Core.dll'),
        (Join-Path $repoRoot 'tests\KPatcher.Tests\bin\Release\net9.0\KPatcher.Core.dll'),
        (Join-Path $repoRoot 'src\KPatcher.Core\bin\Debug\net9.0\KPatcher.Core.dll'),
        (Join-Path $repoRoot 'src\KPatcher.Core\bin\Release\net9.0\KPatcher.Core.dll')
    )

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            Add-Type -Path $candidate
            return
        }
    }

    throw 'KPatcher.Core.dll not found. Build KPatcher.Tests or KPatcher.Core first so capsule extraction types are available.'
}

function Get-IniSectionTable {
    param([Parameter(Mandatory = $true)][string] $IniPath)

    $sections = @{}
    $currentSection = $null

    foreach ($line in Get-Content -LiteralPath $IniPath) {
        $trimmed = $line.Trim()
        if ([string]::IsNullOrWhiteSpace($trimmed) -or $trimmed.StartsWith(';')) {
            continue
        }

        if ($trimmed.StartsWith('[') -and $trimmed.EndsWith(']')) {
            $currentSection = $trimmed.Substring(1, $trimmed.Length - 2)
            if (-not $sections.ContainsKey($currentSection)) {
                $sections[$currentSection] = @{}
            }
            continue
        }

        if ($null -eq $currentSection) {
            continue
        }

        $delimiterIndex = $trimmed.IndexOf('=')
        if ($delimiterIndex -lt 0) {
            continue
        }

        $key = $trimmed.Substring(0, $delimiterIndex).Trim()
        $value = $trimmed.Substring($delimiterIndex + 1).Trim()
        $sections[$currentSection][$key] = $value
    }

    return $sections
}

function Get-GffExtractionEntries {
    param([Parameter(Mandatory = $true)][string] $IniPath)

    $sections = Get-IniSectionTable -IniPath $IniPath
    if (-not $sections.ContainsKey('GFFList')) {
        return @()
    }

    $entries = New-Object System.Collections.Generic.List[object]
    foreach ($pair in $sections['GFFList'].GetEnumerator() | Sort-Object Key) {
        $outputFileName = $pair.Value
        if ([string]::IsNullOrWhiteSpace($outputFileName) -or -not $sections.ContainsKey($outputFileName)) {
            continue
        }

        $section = $sections[$outputFileName]
        if (-not $section.ContainsKey('!Destination')) {
            continue
        }

        $destination = $section['!Destination'].Replace('/', '\')
        $moduleName = Split-Path -Path $destination -Leaf
        $sourceFileName = if ($section.ContainsKey('!Filename') -and -not [string]::IsNullOrWhiteSpace($section['!Filename'])) {
            $section['!Filename']
        }
        else {
            $outputFileName
        }

        $entries.Add([PSCustomObject]@{
            OutputFileName = $outputFileName
            ModuleName = $moduleName
            SourceFileName = $sourceFileName
            SourceResName = [System.IO.Path]::GetFileNameWithoutExtension($sourceFileName)
            SourceExtension = [System.IO.Path]::GetExtension($sourceFileName).TrimStart('.')
        })
    }

    return $entries
}

Ensure-KPatcherCoreLoaded
$allTypes = [AppDomain]::CurrentDomain.GetAssemblies() |
    ForEach-Object {
        try {
            $_.GetTypes()
        }
        catch {
            @()
        }
    }
$capsuleType = $allTypes | Where-Object { $_.FullName -eq 'KPatcher.Core.Common.Capsule.Capsule' } | Select-Object -First 1
$resourceTypeType = $allTypes | Where-Object { $_.FullName -eq 'KPatcher.Core.Resources.ResourceType' } | Select-Object -First 1
if ($null -eq $capsuleType -or $null -eq $resourceTypeType) {
    throw 'Could not resolve KPatcher.Core capsule/resource types from the loaded assembly.'
}
$modulesRoot = Resolve-K1ModulesPath -GameRoot $K1GameRoot
$extracted = 0

foreach ($iniPath in $IniPaths) {
    $resolvedIniPath = (Resolve-Path -LiteralPath $iniPath).Path
    $outputDirectory = Split-Path -Path $resolvedIniPath -Parent

    foreach ($entry in Get-GffExtractionEntries -IniPath $resolvedIniPath) {
        $destinationPath = Join-Path $outputDirectory $entry.OutputFileName
        if ((Test-Path -LiteralPath $destinationPath -PathType Leaf) -and -not $OverwriteExisting) {
            continue
        }

        $resourceType = $resourceTypeType::FromExtension($entry.SourceExtension)
        $resourceData = $null
        $winningCapsule = $null

        foreach ($capsulePath in Get-CandidateCapsulePaths -OutputDirectory $outputDirectory -ModuleName $entry.ModuleName -K1ModulesRoot $modulesRoot) {
            $capsule = [Activator]::CreateInstance($capsuleType, @($capsulePath, $false))
            $resourceData = $capsule.GetResource($entry.SourceResName, $resourceType)
            if ($null -ne $resourceData) {
                $winningCapsule = $capsulePath
                break
            }
        }

        if ($null -eq $resourceData) {
            throw "Could not extract $($entry.SourceFileName) for $($entry.OutputFileName) from packaged module or bootstrap root candidates for $($entry.ModuleName)"
        }

        [System.IO.File]::WriteAllBytes($destinationPath, $resourceData)
        Write-Host "Extracted $($entry.OutputFileName) from $(Split-Path -Leaf $winningCapsule)"
        $extracted++
    }
}

Write-Host "Extracted $extracted vanilla-only GFF resource(s)."