<#
.SYNOPSIS
    Generates .g.cs fixture + test files for every uncovered real-mod case.
    Reads from extracted tslpatchdata directories, Base64-encodes all binary,
    scrubs identifying information, and produces meticulous test classes.
#>
[CmdletBinding()]
param(
    [string]$BaseDir = (Join-Path $PSScriptRoot '..\tests\KPatcher.Tests\test_files\integration_tslpatcher_mods'),
    [string]$OutputDir = (Join-Path $PSScriptRoot '..\tests\KPatcher.Tests\Integration'),
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

# ── Covered case IDs (already have test files) ─────────────────────────
$coveredCases = @(
    "k1_p002","k1_p004","k1_p006","k1_p007","k1_p008","k1_p009","k1_p010","k1_p011","k1_p012",
    "k1_p014","k1_p015","k1_p016","k1_p017","k1_p027","k1_p044","k1_p053","k1_p054","k1_p056",
    "k1_p058","k1_p061","k1_p063","k1_p066",
    "tsl_p024","tsl_p025","tsl_p026","tsl_p035","tsl_p056"
)

# ── Load inventory ──────────────────────────────────────────────────────
$inventoryPath = Join-Path $BaseDir 'inventory\inventory.json'
if (-not (Test-Path $inventoryPath)) { throw "Inventory not found: $inventoryPath" }
$inventory = @(Get-Content $inventoryPath -Raw | ConvertFrom-Json)

# ── GFF extensions (round-trip via GFF.FromBytes) ───────────────────────
$gffExtensions = @('.dlg','.utc','.uti','.utp','.utd','.uts','.utw','.utt','.git','.are','.ifo','.pth','.jrl','.fac','.bic')

# ── Text scrub regexes ─────────────────────────────────────────────────
function Invoke-TextScrub([string]$text) {
    $result = $text
    $result = [regex]::Replace($result, '(?i)https?://(?:www\.)?deadlystream\.com\S*', 'https://example.invalid/mod-source')
    $result = [regex]::Replace($result, '(?i)https?://(?:www\.)?dropbox\.com\S*', 'https://example.invalid/file-host')
    $result = [regex]::Replace($result, '(?i)https?://(?:www\.)?github\.com\S*', 'https://example.invalid/code-host')
    $result = [regex]::Replace($result, '(?i)deadlystream', 'project-source')
    $result = [regex]::Replace($result, '(?i)dropbox', 'file-host')
    $result = [regex]::Replace($result, '(?im)^(\s*(?:author|authors|mod author|mod authors|created by|made by|credits?)\s*[:=-]\s*).*$', '$1[redacted]')
    $result = [regex]::Replace($result, '(?im)^(\s*(?:original mod(?:ification)? by|inspired by|ported by|requested by|special thanks(?: to)?|thanks to)\s*[:=-]\s*).*$', '$1[redacted]')
    $result = [regex]::Replace($result, '(?im)^(\s*by\s+).*$', '$1[redacted]')
    return $result
}

# ── INI parsing helper ──────────────────────────────────────────────────
function Parse-ChangesIni([string]$iniContent) {
    $sections = [ordered]@{}
    $currentSection = $null
    foreach ($rawLine in $iniContent -split "`r?`n") {
        $line = $rawLine.Trim()
        if ($line -match '^\[(.+)\]$') {
            $currentSection = $Matches[1]
            if (-not $sections.Contains($currentSection)) {
                $sections[$currentSection] = [ordered]@{}
            }
        } elseif ($currentSection -and $line -and -not $line.StartsWith(';')) {
            $eqIdx = $line.IndexOf('=')
            if ($eqIdx -gt 0) {
                $key = $line.Substring(0, $eqIdx)
                $val = $line.Substring($eqIdx + 1)
                $sections[$currentSection][$key] = $val
            }
        }
    }
    return $sections
}

function Get-IniSectionCount($sections, [string]$sectionName) {
    if (-not $sections.Contains($sectionName)) { return 0 }
    $sect = $sections[$sectionName]
    return ($sect.Keys | Where-Object { $_ -match '^\w+\d+$' }).Count
}

# ── PascalCase helper ───────────────────────────────────────────────────
function ConvertTo-PascalCase([string]$caseId) {
    # k1_p000 → K1P000
    ($caseId -replace '_','') -replace '^(.)', { $_.Groups[1].Value.ToUpper() } -replace '(p)(\d)', { $_.Groups[1].Value.ToUpper() + $_.Groups[2].Value }
}

function Get-SafePropertyName([string]$fileName) {
    $name = [IO.Path]::GetFileNameWithoutExtension($fileName)
    $ext = [IO.Path]::GetExtension($fileName).TrimStart('.').ToUpper()
    # Clean chars and make PascalCase
    $clean = ($name -replace '[^a-zA-Z0-9_]','_') -replace '(^|_)(\w)', { $_.Groups[2].Value.ToUpper() }
    # C# identifiers cannot start with a digit
    if ($clean -match '^\d') { $clean = "_$clean" }
    return "${clean}${ext}Bytes"
}

function Get-FileWriteMethod([string]$ext) {
    $lower = $ext.ToLowerInvariant()
    if ($lower -in $gffExtensions) { return 'WriteGffFile' }
    return 'WriteBinaryFile'
}

# ── C# string escaping ─────────────────────────────────────────────────
function Escape-CSharpString([string]$text) {
    $text.Replace('\','\\').Replace('"','\"').Replace("`r`n","\r\n").Replace("`n","\n").Replace("`r","\r")
}

# ── Main generation ─────────────────────────────────────────────────────
$generatedDir = Join-Path $OutputDir 'Generated'
if (-not (Test-Path $generatedDir)) { New-Item -ItemType Directory -Path $generatedDir -Force | Out-Null }

$generatedCount = 0
$skippedMissing = 0

foreach ($row in $inventory | Where-Object { $_.Supported -and $_.CaseId -notin $coveredCases }) {
    $caseId = $row.CaseId
    $sourceGame = $row.SourceGame  # K1 or TSL
    $pascal = ConvertTo-PascalCase $caseId
    
    # Check if already exists
    $testFile = Join-Path $OutputDir "${pascal}Tests.cs"
    $fixtureFile = Join-Path $generatedDir "${pascal}Fixture.g.cs"
    if ((-not $Force) -and (Test-Path $testFile)) {
        Write-Host "SKIP (test exists): $caseId"
        continue
    }
    
    # Find tslpatchdata
    $tslDir = Join-Path $BaseDir $row.TslpatchdataRelativePath.Replace('/','\')
    if (-not (Test-Path $tslDir)) {
        Write-Host "SKIP (missing tslpatchdata): $caseId"
        $skippedMissing++
        continue
    }
    
    # Read changes.ini
    $changesIniPath = Join-Path $tslDir $row.PrimaryEntryRelativePath
    if (-not (Test-Path $changesIniPath)) {
        Write-Host "SKIP (no changes.ini): $caseId"
        $skippedMissing++
        continue
    }
    $changesIniRaw = Get-Content $changesIniPath -Raw -Encoding UTF8
    $changesIniScrubbed = Invoke-TextScrub $changesIniRaw
    $iniSections = Parse-ChangesIni $changesIniScrubbed
    
    # Get settings
    $settingsSection = if ($iniSections.Contains('Settings')) { $iniSections['Settings'] } else { @{} }
    $gameNumber = if ($settingsSection.Contains('LookupGameNumber')) { $settingsSection['LookupGameNumber'] } else { if ($sourceGame -eq 'K1') { '1' } else { '2' } }
    $isK1 = $gameNumber -eq '1' -or $sourceGame -eq 'K1'
    
    # Count sections
    $installListCount = $row.InstallListKeyCount
    $twodaListCount = $row.TwoDAListKeyCount
    $gffListCount = $row.GFFListKeyCount
    $ssfListCount = $row.SSFListKeyCount
    $tlkListCount = $row.TLKListKeyCount
    $compileListCount = $row.CompileListKeyCount
    $hackListCount = $row.HackListKeyCount
    
    # Enumerate tslpatchdata files (recursive, relative)
    $allFiles = @()
    $tslDirNorm = (Resolve-Path $tslDir).Path.TrimEnd('\')
    foreach ($f in Get-ChildItem $tslDirNorm -File -Recurse) {
        $relPath = $f.FullName.Substring($tslDirNorm.Length + 1)
        $ext = $f.Extension.ToLowerInvariant()
        $size = $f.Length
        $allFiles += @{
            RelPath = $relPath
            FullPath = $f.FullName
            Extension = $ext
            Size = $size
            FileName = $f.Name
        }
    }
    
    # Separate text vs binary files (limit huge files)
    $textExts = @('.ini','.cfg','.nss','.txt')
    $maxBinarySize = 512 * 1024  # 512KB per file limit for inline
    
    # Parse InstallList folder/file mappings
    $installFolders = [ordered]@{}
    if ($iniSections.Contains('InstallList')) {
        $installSect = $iniSections['InstallList']
        foreach ($key in $installSect.Keys) {
            if ($key -match '^install_folder(\d+)$') {
                $idx = $Matches[1]
                $dest = $installSect[$key]
                $folderSectName = "install_folder$idx"
                $installFiles = @()
                if ($iniSections.Contains($folderSectName)) {
                    $folderSect = $iniSections[$folderSectName]
                    foreach ($fkey in $folderSect.Keys) {
                        if ($fkey -match '^File\d+$') {
                            $installFiles += $folderSect[$fkey]
                        }
                    }
                }
                $installFolders[$folderSectName] = @{
                    Destination = $dest
                    Files = $installFiles
                }
            }
        }
    }
    
    # Parse 2DA table references
    $twodaTables = @()
    if ($iniSections.Contains('2DAList')) {
        foreach ($key in $iniSections['2DAList'].Keys) {
            if ($key -match '^Table\d+$') { $twodaTables += $iniSections['2DAList'][$key] }
        }
    }
    
    # Parse GFF references
    $gffTargets = @()
    if ($iniSections.Contains('GFFList')) {
        foreach ($key in $iniSections['GFFList'].Keys) {
            if ($key -match '^File\d+$') { $gffTargets += $iniSections['GFFList'][$key] }
        }
    }
    
    # ================================================================
    #  GENERATE FIXTURE .g.cs
    # ================================================================
    $sb = [System.Text.StringBuilder]::new()
    [void]$sb.AppendLine("// <auto-generated/>")
    [void]$sb.AppendLine("using System;")
    [void]$sb.AppendLine("using System.Collections.Generic;")
    [void]$sb.AppendLine("using System.IO;")
    [void]$sb.AppendLine("using System.Text;")
    [void]$sb.AppendLine("using KPatcher.Core.Formats.ERF;")
    [void]$sb.AppendLine("using KPatcher.Core.Formats.GFF;")
    [void]$sb.AppendLine("using KPatcher.Core.Resources;")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("namespace KPatcher.Core.Tests.Integration")
    [void]$sb.AppendLine("{")
    [void]$sb.AppendLine("    internal static class ${pascal}Fixture")
    [void]$sb.AppendLine("    {")
    [void]$sb.AppendLine("        internal const string CaseId = `"$caseId`";")
    [void]$sb.AppendLine("")
    
    # Track which files are embedded vs stubbed
    $embeddedFiles = @{}  # relPath → propertyName
    $stubbedFiles = @()
    
    foreach ($fileInfo in $allFiles) {
        $relPath = $fileInfo.RelPath
        $ext = $fileInfo.Extension
        $size = $fileInfo.Size
        $fileName = $fileInfo.FileName
        
        # Skip .exe files
        if ($ext -eq '.exe') { continue }
        
        $propName = Get-SafePropertyName $relPath.Replace('\','_')
        # Ensure unique property name
        $origProp = $propName
        $counter = 2
        while ($embeddedFiles.Values -contains $propName) {
            $propName = "${origProp}_${counter}"
            $counter++
        }
        
        if ($ext -in $textExts -or $ext -eq '.rtf') {
            # Text file → string constant
            $content = Get-Content $fileInfo.FullPath -Raw -Encoding UTF8
            # Scrub identifying info from text files
            $content = Invoke-TextScrub $content
            $textPropName = $propName -replace 'Bytes$','Text'
            $escapedContent = Escape-CSharpString $content
            
            # Use verbatim string for long content
            if ($escapedContent.Length -gt 200) {
                $verbatim = $content.Replace('"','""')
                [void]$sb.AppendLine("        internal const string $textPropName =")
                [void]$sb.AppendLine("            @`"$verbatim`";")
            } else {
                [void]$sb.AppendLine("        internal const string $textPropName = `"$escapedContent`";")
            }
            [void]$sb.AppendLine("")
            $embeddedFiles[$relPath] = $textPropName
        }
        elseif ($size -le $maxBinarySize) {
            # Binary file → Base64
            $bytes = [IO.File]::ReadAllBytes($fileInfo.FullPath)
            $b64 = [Convert]::ToBase64String($bytes)
            
            $b64PropName = $propName -replace 'Bytes$','Base64'
            [void]$sb.AppendLine("        private const string $b64PropName =")
            
            # Split long base64 into chunks
            $chunkSize = 100
            $chunks = @()
            if ($b64.Length -eq 0) {
                $chunks += '""'
            } else {
                for ($i = 0; $i -lt $b64.Length; $i += $chunkSize) {
                    $len = [Math]::Min($chunkSize, $b64.Length - $i)
                    $chunks += "`"$($b64.Substring($i, $len))`""
                }
            }
            [void]$sb.AppendLine("            $($chunks -join " +`r`n            ");")
            [void]$sb.AppendLine("")
            [void]$sb.AppendLine("        internal static byte[] $propName => Convert.FromBase64String($b64PropName);")
            [void]$sb.AppendLine("")
            $embeddedFiles[$relPath] = $propName
        }
        else {
            # Too large → will be stubbed
            $stubbedFiles += $relPath
            Write-Host "  STUB (${size}B > ${maxBinarySize}B): $caseId / $relPath"
        }
    }
    
    # ── Materialize method ──
    [void]$sb.AppendLine("        internal static void Materialize(string modRoot, string tslRoot)")
    [void]$sb.AppendLine("        {")
    [void]$sb.AppendLine("            _ = modRoot;")
    [void]$sb.AppendLine("            Directory.CreateDirectory(tslRoot);")
    
    foreach ($fileInfo in $allFiles) {
        $relPath = $fileInfo.RelPath
        $ext = $fileInfo.Extension
        $fileName = $fileInfo.FileName
        
        if ($ext -eq '.exe') { continue }
        
        # Ensure subdirectory exists
        $subDir = [IO.Path]::GetDirectoryName($relPath)
        if ($subDir) {
            $cleanSubDir = $subDir.Replace('\','\\')
            [void]$sb.AppendLine("            Directory.CreateDirectory(Path.Combine(tslRoot, `"$cleanSubDir`"));")
        }
        
        $cleanRelPath = $relPath.Replace('\','\\')
        
        if ($embeddedFiles.Contains($relPath)) {
            $prop = $embeddedFiles[$relPath]
            if ($prop -match 'Text$') {
                [void]$sb.AppendLine("            File.WriteAllText(Path.Combine(tslRoot, `"$cleanRelPath`"), $prop, new UTF8Encoding(false));")
            }
            elseif ($ext -in $gffExtensions) {
                [void]$sb.AppendLine("            WriteGffFile(Path.Combine(tslRoot, `"$cleanRelPath`"), $prop);")
            }
            else {
                [void]$sb.AppendLine("            File.WriteAllBytes(Path.Combine(tslRoot, `"$cleanRelPath`"), $prop);")
            }
        }
        elseif ($relPath -in $stubbedFiles) {
            # Write a minimal stub
            [void]$sb.AppendLine("            File.WriteAllBytes(Path.Combine(tslRoot, `"$cleanRelPath`"), new byte[] { 0x00 });")
        }
    }
    
    [void]$sb.AppendLine("        }")
    [void]$sb.AppendLine("")
    
    # ── Helper methods ──
    $hasGff = ($allFiles | Where-Object { $_.Extension -in $gffExtensions -and $embeddedFiles.Contains($_.RelPath) }).Count -gt 0
    if ($hasGff) {
        [void]$sb.AppendLine("        private static void WriteGffFile(string path, byte[] raw)")
        [void]$sb.AppendLine("        {")
        [void]$sb.AppendLine("            File.WriteAllBytes(path, GFF.FromBytes(raw).ToBytes());")
        [void]$sb.AppendLine("        }")
        [void]$sb.AppendLine("")
    }
    
    [void]$sb.AppendLine("    }")
    [void]$sb.AppendLine("}")
    
    Set-Content -LiteralPath $fixtureFile -Value $sb.ToString() -Encoding UTF8 -NoNewline
    
    # ================================================================
    #  GENERATE TEST .cs
    # ================================================================
    $tb = [System.Text.StringBuilder]::new()
    [void]$tb.AppendLine("using System;")
    [void]$tb.AppendLine("using System.IO;")
    [void]$tb.AppendLine("using System.Linq;")
    [void]$tb.AppendLine("using FluentAssertions;")
    [void]$tb.AppendLine("using IniParser.Model;")
    [void]$tb.AppendLine("using KPatcher.Core.Common;")
    [void]$tb.AppendLine("using KPatcher.Core.Common.Capsule;")
    [void]$tb.AppendLine("using KPatcher.Core.Config;")
    [void]$tb.AppendLine("using KPatcher.Core.Formats.GFF;")
    [void]$tb.AppendLine("using KPatcher.Core.Formats.TwoDA;")
    [void]$tb.AppendLine("using KPatcher.Core.Formats.TLK;")
    [void]$tb.AppendLine("using KPatcher.Core.Logger;")
    [void]$tb.AppendLine("using KPatcher.Core.Patcher;")
    [void]$tb.AppendLine("using KPatcher.Core.Reader;")
    [void]$tb.AppendLine("using KPatcher.Core.Resources;")
    [void]$tb.AppendLine("using Xunit;")
    [void]$tb.AppendLine("")
    [void]$tb.AppendLine("namespace KPatcher.Core.Tests.Integration")
    [void]$tb.AppendLine("{")
    [void]$tb.AppendLine("    public sealed class ${pascal}Tests")
    [void]$tb.AppendLine("    {")
    [void]$tb.AppendLine("        [Fact]")
    [void]$tb.AppendLine("        public void ${pascal}_materializes_and_installs_real_payloads()")
    [void]$tb.AppendLine("        {")
    
    $stubParam = if ($stubbedFiles.Count -gt 0) { 'true' } else { 'false' }
    $gameType = if ($isK1) { '"K1"' } else { '"TSL"' }
    
    [void]$tb.AppendLine("            ScenarioPatternModInstallHarness.RunSingleIniSyntheticInstallFromMaterializedMod(")
    [void]$tb.AppendLine("                `"$caseId`",")
    [void]$tb.AppendLine("                $gameType,")
    [void]$tb.AppendLine("                (modRoot, tslRoot) =>")
    [void]$tb.AppendLine("                {")
    [void]$tb.AppendLine("                    ${pascal}Fixture.Materialize(modRoot, tslRoot);")
    [void]$tb.AppendLine("                    AssertMaterializedPayload(tslRoot);")
    [void]$tb.AppendLine("                    AssertConfigReaderOutputs(tslRoot);")
    [void]$tb.AppendLine("                },")
    [void]$tb.AppendLine("                `"$($row.PrimaryEntryRelativePath.Replace('\','/'))`",")
    
    # prepareSyntheticGame: seed baseline 2DA/GFF/modules
    $needsGameSeed = ($twodaTables.Count -gt 0) -or ($gffListCount -gt 0)
    if ($needsGameSeed) {
        [void]$tb.AppendLine("                (gameRoot, modRootFull, tslPatchData) =>")
        [void]$tb.AppendLine("                {")
        
        # Seed 2DA baselines
        foreach ($table in $twodaTables) {
            $tableName = $table.ToLowerInvariant()
            # Check if the mod ships its own 2DA (source file)
            $modShips2DA = $allFiles | Where-Object { $_.FileName.ToLowerInvariant() -eq $tableName }
            if (-not $modShips2DA) {
                $cleanTable = $tableName.Replace('.2da','')
                [void]$tb.AppendLine("                    // Seed baseline $tableName for patching")
                [void]$tb.AppendLine("                    var ${cleanTable}Table = new TwoDA(new System.Collections.Generic.List<string> { `"label`" });")
                
                # Find what rows are referenced in the ini
                if ($iniSections.Contains($tableName)) {
                    $patchSect = $iniSections[$tableName]
                    $maxRow = 0
                    foreach ($k in $patchSect.Keys) {
                        if ($k -match '^ChangeRow\d+$') {
                            $rowTarget = $patchSect[$k]
                            if ($iniSections.Contains($rowTarget)) {
                                $rowSect = $iniSections[$rowTarget]
                                if ($rowSect.Contains('RowLabel')) {
                                    $rowNum = 0
                                    if ([int]::TryParse($rowSect['RowLabel'], [ref]$rowNum) -and $rowNum -gt $maxRow) {
                                        $maxRow = $rowNum
                                    }
                                }
                            }
                        }
                        if ($k -match '^AddRow\d+$') { $maxRow = [Math]::Max($maxRow, 10) }
                    }
                    for ($r = 0; $r -le $maxRow; $r++) {
                        [void]$tb.AppendLine("                    ${cleanTable}Table.AddRow(`"$r`", new System.Collections.Generic.Dictionary<string, object> { [`"label`"] = `"$r`" });")
                    }
                }
                
                [void]$tb.AppendLine("                    StrictFixtureBuilder.WriteGameOverrideTwoDA(gameRoot, `"$tableName`", ${cleanTable}Table);")
            }
        }
        
        # Copy modules from tslpatchdata to game if GFF patches target modules
        foreach ($gffTarget in $gffTargets) {
            if ($iniSections.Contains($gffTarget)) {
                $gffSect = $iniSections[$gffTarget]
                if ($gffSect.Contains('!Destination')) {
                    $dest = $gffSect['!Destination']
                    if ($dest -match '(?i)modules') {
                        [void]$tb.AppendLine("                    ScenarioPatternModInstallHarness.CopyModsFromTslPatchDataModulesToGame(gameRoot, tslPatchData);")
                        break
                    }
                }
            }
        }
        
        [void]$tb.AppendLine("                },")
    }
    else {
        [void]$tb.AppendLine("                null,")
    }
    
    # assertGameAfterVerifiedSuccessfulInstall
    $hasInstallFiles = $installFolders.Count -gt 0
    $hasTLK = $tlkListCount -gt 0
    $has2DA = $twodaTables.Count -gt 0
    $hasGFF = $gffListCount -gt 0
    
    if ($hasInstallFiles -or $hasTLK -or $has2DA) {
        [void]$tb.AppendLine("                assertGameAfterVerifiedSuccessfulInstall: gameRoot =>")
        [void]$tb.AppendLine("                {")
        
        # Assert installed files
        foreach ($folderName in $installFolders.Keys) {
            $folder = $installFolders[$folderName]
            $dest = $folder.Destination
            foreach ($installFileName in $folder.Files) {
                $destDir = if ($dest -match '(?i)^override$') { 'Override' } elseif ($dest -match '(?i)^modules$') { 'Modules' } else { $dest }
                $escapedDestDir = $destDir.Replace('\','\\')
                [void]$tb.AppendLine("                    File.Exists(Path.Combine(gameRoot, `"$escapedDestDir`", `"$installFileName`"))")
                [void]$tb.AppendLine("                        .Should().BeTrue(`"$installFileName should be installed to $escapedDestDir`");")
                
                # Byte equality for embedded files
                $matchFile = $allFiles | Where-Object { $_.FileName -eq $installFileName }
                if ($matchFile -and $embeddedFiles.Contains($matchFile.RelPath)) {
                    $prop = $embeddedFiles[$matchFile.RelPath]
                    if ($prop -notmatch 'Text$') {
                        if ($matchFile.Extension -in $gffExtensions) {
                            [void]$tb.AppendLine("                    File.ReadAllBytes(Path.Combine(gameRoot, `"$escapedDestDir`", `"$installFileName`"))")
                            [void]$tb.AppendLine("                        .Should().Equal(GFF.FromBytes(${pascal}Fixture.$prop).ToBytes());")
                        } else {
                            [void]$tb.AppendLine("                    File.ReadAllBytes(Path.Combine(gameRoot, `"$escapedDestDir`", `"$installFileName`"))")
                            [void]$tb.AppendLine("                        .Should().Equal(${pascal}Fixture.$prop);")
                        }
                    }
                }
            }
        }
        
        # Assert TLK modifications
        if ($hasTLK) {
            [void]$tb.AppendLine("                    // Verify TLK was patched")
            [void]$tb.AppendLine("                    var dialog = TLK.FromFile(Path.Combine(gameRoot, `"dialog.tlk`"));")
            [void]$tb.AppendLine("                    dialog.Count.Should().BeGreaterThan(1, `"TLK should have appended entries`");")
        }
        
        # Assert 2DA patches
        foreach ($table in $twodaTables) {
            $tableName = $table.ToLowerInvariant()
            [void]$tb.AppendLine("                    // Verify $tableName was patched")
            [void]$tb.AppendLine("                    File.Exists(Path.Combine(gameRoot, `"Override`", `"$tableName`")).Should().BeTrue();")
            [void]$tb.AppendLine("                    var installed_$($tableName.Replace('.2da','')) = TwoDA.FromBytes(File.ReadAllBytes(Path.Combine(gameRoot, `"Override`", `"$tableName`")));")
            
            # Check specific row patches
            if ($iniSections.Contains($tableName)) {
                $patchSect = $iniSections[$tableName]
                foreach ($k in $patchSect.Keys) {
                    if ($k -match '^ChangeRow(\d+)$') {
                        $rowTarget = $patchSect[$k]
                        if ($iniSections.Contains($rowTarget)) {
                            $rowSect = $iniSections[$rowTarget]
                            if ($rowSect.Contains('RowLabel')) {
                                $rowLabel = $rowSect['RowLabel']
                                foreach ($colKey in $rowSect.Keys) {
                                    if ($colKey -ne 'RowLabel' -and $colKey -ne 'LabelIndex' -and $colKey -ne 'ExclusiveColumn' -and -not $colKey.StartsWith('2DAMEMORY')) {
                                        $colVal = $rowSect[$colKey]
                                        # Skip StrRef replacements (values like 2DAMemoryN) and complex expressions
                                        if ($colVal -notmatch '^2DAMEMORY|^StrRef|^%%|high\d+$') {
                                            $cleanTableVar = $tableName.Replace('.2da','')
                                            [void]$tb.AppendLine("                    installed_${cleanTableVar}.GetCellString($rowLabel, `"$colKey`").Should().Be(`"$colVal`");")
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        [void]$tb.AppendLine("                },")
    }
    else {
        [void]$tb.AppendLine("                assertGameAfterVerifiedSuccessfulInstall: null,")
    }
    
    [void]$tb.AppendLine("                stubMissingInstallListSources: $stubParam")
    [void]$tb.AppendLine("            );")
    [void]$tb.AppendLine("        }")
    [void]$tb.AppendLine("")
    
    # ── AssertMaterializedPayload ──
    [void]$tb.AppendLine("        private static void AssertMaterializedPayload(string tslRoot)")
    [void]$tb.AppendLine("        {")
    foreach ($fileInfo in $allFiles) {
        if ($fileInfo.Extension -eq '.exe') { continue }
        $relPath = $fileInfo.RelPath.Replace('\','\\')
        [void]$tb.AppendLine("            File.Exists(Path.Combine(tslRoot, `"$relPath`")).Should().BeTrue();")
        
        # Byte equality for embedded binary files
        if ($embeddedFiles.Contains($fileInfo.RelPath)) {
            $prop = $embeddedFiles[$fileInfo.RelPath]
            if ($prop -notmatch 'Text$' -and $fileInfo.Extension -notin $textExts -and $fileInfo.Extension -ne '.rtf') {
                if ($fileInfo.Extension -in $gffExtensions) {
                    [void]$tb.AppendLine("            File.ReadAllBytes(Path.Combine(tslRoot, `"$relPath`"))")
                    [void]$tb.AppendLine("                .Should().Equal(GFF.FromBytes(${pascal}Fixture.$prop).ToBytes());")
                } else {
                    [void]$tb.AppendLine("            File.ReadAllBytes(Path.Combine(tslRoot, `"$relPath`"))")
                    [void]$tb.AppendLine("                .Should().Equal(${pascal}Fixture.$prop);")
                }
            }
        }
    }
    [void]$tb.AppendLine("        }")
    [void]$tb.AppendLine("")
    
    # ── AssertConfigReaderOutputs ──
    [void]$tb.AppendLine("        private static void AssertConfigReaderOutputs(string tslRoot)")
    [void]$tb.AppendLine("        {")
    [void]$tb.AppendLine("            string iniPath = Path.Combine(tslRoot, `"$($row.PrimaryEntryRelativePath.Replace('\','\\'))`");")
    [void]$tb.AppendLine("            var logger = new PatchLogger();")
    [void]$tb.AppendLine("            IniData ini = ConfigReader.LoadAndParseIni(iniPath, caseInsensitive: false);")
    [void]$tb.AppendLine("            var reader = new ConfigReader(ini, tslRoot, logger, tslRoot);")
    [void]$tb.AppendLine("            PatcherConfig cfg = reader.Load(new PatcherConfig());")
    [void]$tb.AppendLine("")
    
    # Assert InstallList count and details
    $totalInstallFiles = 0
    foreach ($fName in $installFolders.Keys) { $totalInstallFiles += $installFolders[$fName].Files.Count }
    [void]$tb.AppendLine("            cfg.InstallList.Should().HaveCount($totalInstallFiles);")
    
    if ($totalInstallFiles -gt 0) {
        $destList = @()
        $srcList = @()
        foreach ($folderName in $installFolders.Keys) {
            $folder = $installFolders[$folderName]
            foreach ($installFile in $folder.Files) {
                $escapedDest = $folder.Destination.Replace('\','\\')
                $destList += "`"$escapedDest`""
                $srcList += "`"$installFile`""
            }
        }
        if ($totalInstallFiles -le 20) {
            [void]$tb.AppendLine("            cfg.InstallList.Select(f => f.SourceFile)")
            [void]$tb.AppendLine("                .Should().Equal($($srcList -join ', '));")
            [void]$tb.AppendLine("            cfg.InstallList.Select(f => f.Destination)")
            [void]$tb.AppendLine("                .Should().Equal($($destList -join ', '));")
        }
    }
    
    # Assert other sections
    [void]$tb.AppendLine("            cfg.Patches2DA.Should().HaveCount($twodaListCount);")
    if ($twodaTables.Count -gt 0 -and $twodaTables.Count -le 10) {
        $tableNames = $twodaTables | ForEach-Object { "`"$_`"" }
        [void]$tb.AppendLine("            cfg.Patches2DA.Select(p => p.SourceFile)")
        [void]$tb.AppendLine("                .Should().Equal($($tableNames -join ', '));")
    }
    
    [void]$tb.AppendLine("            cfg.PatchesGFF.Should().HaveCount($gffListCount);")
    if ($gffTargets.Count -gt 0 -and $gffTargets.Count -le 10) {
        foreach ($i in 0..($gffTargets.Count - 1)) {
            $gffName = $gffTargets[$i]
            [void]$tb.AppendLine("            cfg.PatchesGFF[$i].SourceFile.Should().Be(`"$gffName`");")
            # Get destination from INI
            if ($iniSections.Contains($gffName) -and $iniSections[$gffName].Contains('!Destination')) {
                $gffDest = $iniSections[$gffName]['!Destination']
                [void]$tb.AppendLine("            cfg.PatchesGFF[$i].Destination.Should().Be(@`"$gffDest`");")
            }
        }
    }
    
    [void]$tb.AppendLine("            cfg.PatchesTLK.Modifiers.Should().HaveCount($tlkListCount);")
    [void]$tb.AppendLine("            cfg.PatchesSSF.Should().HaveCount($ssfListCount);")
    [void]$tb.AppendLine("            cfg.PatchesNSS.Should().HaveCount($compileListCount);")
    
    [void]$tb.AppendLine("        }")
    [void]$tb.AppendLine("    }")
    [void]$tb.AppendLine("}")
    
    Set-Content -LiteralPath $testFile -Value $tb.ToString() -Encoding UTF8 -NoNewline
    
    $generatedCount++
    Write-Host "GENERATED: $caseId ($($allFiles.Count) files, $($stubbedFiles.Count) stubbed)"
}

Write-Host "`nDone. Generated $generatedCount test+fixture pairs. Skipped $skippedMissing (missing tslpatchdata)."
