#requires -Version 7.2
<#
.SYNOPSIS
  Builds anonymized TSLPatcher INI corpus folders for ConfigReader pattern tests.

.DESCRIPTION
  Reads DeadlyStream-sourced archives under integration_tslpatcher_mods/deadlystream_k1|deadlystream_tsl,
  extracts only *.ini paths under a tslpatchdata directory (any mod-named parent), merges into a flat
  tslpatchdata tree, scrubs identifying text, drops stub nwnnsscomp.exe where needed for ConfigReader.Load,
  and writes scenario_patterns/manifest.json with generic ids (k1_pNNN / tsl_pNNN).

  Requires 7-Zip CLI (7z) on PATH.

  Note: This overwrites the entire scenario_patterns tree under tests/KPatcher.Tests/EmbeddedIntegrationMods/scenario_patterns.
  Restore the hand-curated ModInstaller smoke folder k1_p000 (and its manifest entry) from git if you rely on TslpatcherPatternModInstallerTests.

.EXAMPLE
  pwsh -NoProfile -File ./scripts/GenerateTslpatcherPatternScenarios.ps1
#>
param(
    [string] $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
    [string] $SevenZip = '7z'
)

$ErrorActionPreference = 'Stop'

function Get-ArchiveIniPaths {
    param([string] $ArchivePath)
    $raw = & $SevenZip l -slt $ArchivePath 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Skipping unreadable archive (7z exit $LASTEXITCODE): $ArchivePath"
        return @()
    }
    $paths = New-Object System.Collections.Generic.List[string]
    foreach ($line in $raw) {
        if ($line -notmatch '^\s*Path\s*=\s*(.+)\s*$') { continue }
        $p = $Matches[1].Trim()
        if ($p -match '\.(zip|7z|rar)$') { continue }
        $norm = $p.Replace('/', '\')
        # Avoid regex like \tslpatchdata ( accidental \t tab escape in some engines )
        if ($norm.Contains('tslpatchdata', [System.StringComparison]::OrdinalIgnoreCase) -and
            $norm.EndsWith('.ini', [System.StringComparison]::OrdinalIgnoreCase)) {
            [void]$paths.Add($norm)
        }
    }
    return $paths | Select-Object -Unique
}

function Merge-TslpatchdataInisToOutput {
    param(
        [string] $StageRoot,
        [string] $DestTslpatchdata
    )
    $tsDirs = Get-ChildItem -LiteralPath $StageRoot -Recurse -Directory -Filter 'tslpatchdata' -ErrorAction SilentlyContinue
    if (-not $tsDirs -or $tsDirs.Count -eq 0) {
        return $false
    }
    # Prefer shallowest tslpatchdata (mod root)
    $td = $tsDirs | Sort-Object { $_.FullName.Length } | Select-Object -First 1
    New-Item -ItemType Directory -Path $DestTslpatchdata -Force | Out-Null
    Get-ChildItem -LiteralPath $td.FullName -Recurse -File -Filter '*.ini' | ForEach-Object {
        $rel = $_.FullName.Substring($td.FullName.Length).TrimStart('\', '/')
        $target = Join-Path $DestTslpatchdata $rel
        $dir = Split-Path -Parent $target
        if (-not (Test-Path -LiteralPath $dir)) {
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
        }
        Copy-Item -LiteralPath $_.FullName -Destination $target -Force
    }
    return $true
}

function Select-PrimaryIni {
    param([string] $TslpatchdataRoot)
    $all = @(Get-ChildItem -LiteralPath $TslpatchdataRoot -Recurse -File -Filter '*.ini' | Sort-Object FullName)
    if ($all.Count -eq 0) { return $null }

    $named = $all | Where-Object { $_.Name -ieq 'changes.ini' } | Select-Object -First 1
    if ($named) { return $named }

    $named = $all | Where-Object { $_.Name -like 'changes*.ini' } | Sort-Object { $_.FullName.Length }, Name | Select-Object -First 1
    if ($named) { return $named }

    foreach ($f in ($all | Where-Object { $_.Name -ine 'namespaces.ini' })) {
        $head = Get-Content -LiteralPath $f.FullName -Raw -ErrorAction SilentlyContinue
        if (-not $head) { continue }
        if ($head -match '(?im)^\[(InstallList|GFFList|2DAList|CompileList|TLKList|HACKList|SSFList)\]') {
            return $f
        }
    }

    $withSettings = $all | Where-Object {
        $_.Name -ine 'namespaces.ini' -and (Select-String -LiteralPath $_.FullName -Pattern '^\[Settings\]' -Quiet -ErrorAction SilentlyContinue)
    } | Select-Object -First 1
    if ($withSettings) { return $withSettings }

    return ($all | Where-Object { $_.Name -ine 'namespaces.ini' } | Select-Object -First 1)
}

function Repair-RequiredAndRequiredMsgCounts {
    param([string] $Text)
    $marker = '[Settings]'
    $sIdx = $Text.IndexOf($marker, [System.StringComparison]::OrdinalIgnoreCase)
    if ($sIdx -lt 0) {
        return $Text
    }
    $fromSettings = $Text.Substring($sIdx)
    $rel = $fromSettings.IndexOf("`n[", [System.StringComparison]::Ordinal)
    if ($rel -lt 0) {
        $rel = $fromSettings.IndexOf("`r`n[", [System.StringComparison]::Ordinal)
    }
    if ($rel -gt 0) {
        $head = $fromSettings.Substring(0, $rel)
        $tail = $fromSettings.Substring($rel)
    }
    else {
        $head = $fromSettings
        $tail = ''
    }
    $reqC = @([regex]::Matches($head, '(?im)^Required(\d+)?\s*=')).Count
    $msgC = @([regex]::Matches($head, '(?im)^RequiredMsg\d*\s*=')).Count
    if ($reqC -le $msgC) {
        return $Text
    }
    $pad = $reqC - $msgC
    $extra = New-Object System.Collections.Generic.List[string]
    for ($i = 0; $i -lt $pad; $i++) {
        $idx = $msgC + $i
        [void]$extra.Add("RequiredMsg$idx=Required files are present.")
    }
    $insert = ($extra -join "`r`n") + "`r`n"
    $newFrom = $head.TrimEnd("`r", "`n") + "`r`n" + $insert + $tail
    return $Text.Substring(0, $sIdx) + $newFrom
}

function Remove-InvalidStandaloneRequiredMsgKeys {
    param([string] $Text)
    $lines = $Text -split "`r?`n"
    $out = New-Object System.Collections.Generic.List[string]
    foreach ($line in $lines) {
        if ($line -match '^\s*([^=]+?)=') {
            $key = $Matches[1].Trim()
            if ($key.Equals('RequiredMsg', [System.StringComparison]::OrdinalIgnoreCase)) {
                continue
            }
        }
        [void]$out.Add($line)
    }
    return ($out -join "`r`n")
}

function Protect-AnonymizedIniText {
    param([string] $Text)
    $t = $Text
    # Window / dialog text
    $t = $t -replace '(?im)^(WindowCaption|WindowTitle)\s*=\s*.*$', '$1=Generic patch pattern'
    $t = $t -replace '(?im)^(ConfirmMessage)\s*=\s*.*$', '$1=Proceed with installation?'
    # URLs and obvious hostnames
    $t = $t -replace 'https?://[^\s;]+', 'https://example.invalid/'
    $t = $t -replace '(?i)\b(deadlystream|nexusmods|moddb|deadlystream\.com|github\.com/[^\s;]+)\b', 'example-host'
    # Namespace option display strings (often author/mod prose)
    $t = $t -replace '(?im)^(Name|Description)\s*=\s*.*$', '$1=Option'
    $t = Remove-InvalidStandaloneRequiredMsgKeys -Text $t
    $t = Repair-RequiredAndRequiredMsgCounts -Text $t
    return $t
}

function Add-NwnnsscompStubs {
    param([string] $TslpatchdataRoot)
    $dirs = @($TslpatchdataRoot) + @(Get-ChildItem -LiteralPath $TslpatchdataRoot -Recurse -Directory | ForEach-Object FullName)
    foreach ($d in $dirs) {
        $hasIni = @(Get-ChildItem -LiteralPath $d -File -Filter '*.ini' -ErrorAction SilentlyContinue).Count -gt 0
        if (-not $hasIni) { continue }
        $exe = Join-Path $d 'nwnnsscomp.exe'
        if (-not (Test-Path -LiteralPath $exe)) {
            [System.IO.File]::WriteAllBytes($exe, [byte[]]@(0x4D, 0x5A))
        }
    }
}

function Process-Archive {
    param(
        [string] $ArchivePath,
        [string] $PatternId,
        [string] $OutScenarioDir
    )
    $iniPaths = @(Get-ArchiveIniPaths -ArchivePath $ArchivePath)
    if ($iniPaths.Count -eq 0) {
        Write-Warning "No tslpatchdata *.ini in archive: $ArchivePath"
        return $null
    }

    $stage = Join-Path ([System.IO.Path]::GetTempPath()) ("kpatcher_pat_" + [Guid]::NewGuid().ToString('n'))
    try {
        New-Item -ItemType Directory -Path $stage | Out-Null
        $listFile = Join-Path $stage '_extract.lst'
        Set-Content -LiteralPath $listFile -Value ($iniPaths -join "`r`n") -Encoding ascii
        $null = & $SevenZip x $ArchivePath "@$listFile" "-o$stage" -y
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "7z extract failed ($LASTEXITCODE): $ArchivePath"
            return $null
        }

        $destTsl = Join-Path $OutScenarioDir 'tslpatchdata'
        if (Test-Path -LiteralPath $OutScenarioDir) {
            Remove-Item -LiteralPath $OutScenarioDir -Recurse -Force
        }
        New-Item -ItemType Directory -Path $OutScenarioDir | Out-Null

        $ok = Merge-TslpatchdataInisToOutput -StageRoot $stage -DestTslpatchdata $destTsl
        if (-not $ok) {
            Write-Warning "No tslpatchdata folder after extract: $ArchivePath"
            return $null
        }

        Get-ChildItem -LiteralPath $destTsl -Recurse -File -Filter '*.ini' | ForEach-Object {
            $txt = Get-Content -LiteralPath $_.FullName -Raw
            $new = Protect-AnonymizedIniText -Text $txt
            $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
            [System.IO.File]::WriteAllText($_.FullName, $new, $utf8NoBom)
        }

        $anyCompile = Get-ChildItem -LiteralPath $destTsl -Recurse -File -Filter '*.ini' |
            Select-String -Pattern '^\[CompileList\]' -Quiet
        if ($anyCompile) {
            Add-NwnnsscompStubs -TslpatchdataRoot $destTsl
        }

        $primary = Select-PrimaryIni -TslpatchdataRoot $destTsl
        if (-not $primary) {
            Write-Warning "Could not pick primary INI: $ArchivePath"
            return $null
        }
        $rel = $primary.FullName.Substring($destTsl.Length).TrimStart('\', '/')
        return [pscustomobject]@{
            Id         = $PatternId
            ChangesIni = $rel -replace '\\', '/'
        }
    }
    finally {
        if (Test-Path -LiteralPath $stage) {
            Remove-Item -LiteralPath $stage -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

$integration = Join-Path $RepoRoot 'tests/KPatcher.Tests/test_files/integration_tslpatcher_mods'
$k1Dir = Join-Path $integration 'deadlystream_k1'
$tslDir = Join-Path $integration 'deadlystream_tsl'
$outBase = Join-Path $RepoRoot 'tests/KPatcher.Tests/EmbeddedIntegrationMods/scenario_patterns'

if (-not (Get-Command $SevenZip -ErrorAction SilentlyContinue)) {
    throw "7-Zip CLI '$SevenZip' not found on PATH."
}

New-Item -ItemType Directory -Path $outBase -Force | Out-Null

$manifest = New-Object System.Collections.Generic.List[object]
$idxK1 = 1
$idxTsl = 1

foreach ($zip in (Get-ChildItem -LiteralPath $k1Dir -File | Where-Object { $_.Extension -match '^\.(zip|7z|rar)$' } | Sort-Object Name)) {
    $id = 'k1_p{0:000}' -f $idxK1
    $scenarioDir = Join-Path $outBase $id
    $entry = Process-Archive -ArchivePath $zip.FullName -PatternId $id -OutScenarioDir $scenarioDir
    if ($entry) {
        [void]$manifest.Add([pscustomobject]@{
                Id                 = $entry.Id
                ChangesIniRelative = $entry.ChangesIni
                SourceGame         = 'K1'
            })
        $idxK1++
    }
}

foreach ($zip in (Get-ChildItem -LiteralPath $tslDir -File | Where-Object { $_.Extension -match '^\.(zip|7z|rar)$' } | Sort-Object Name)) {
    $id = 'tsl_p{0:000}' -f $idxTsl
    $scenarioDir = Join-Path $outBase $id
    $entry = Process-Archive -ArchivePath $zip.FullName -PatternId $id -OutScenarioDir $scenarioDir
    if ($entry) {
        [void]$manifest.Add([pscustomobject]@{
                Id                 = $entry.Id
                ChangesIniRelative = $entry.ChangesIni
                SourceGame         = 'TSL'
            })
        $idxTsl++
    }
}

$manifestPath = Join-Path $outBase 'manifest.json'
$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $manifestPath -Encoding utf8
Write-Host "Wrote $($manifest.Count) scenarios under $outBase"
Write-Host "Manifest: $manifestPath"
