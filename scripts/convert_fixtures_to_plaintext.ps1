$ErrorActionPreference = "Continue"
$fixBase = "c:\GitHub\KPatcher\tests\KPatcher.Tests\test_files\integration_tslpatcher_mods\fixtures"
$outBase = "c:\GitHub\KPatcher\tmp_dump\fixture_plaintext"
$gffExts = @('.utc','.uti','.utp','.utd','.uts','.dlg','.git','.are','.ifo')
$skipExts = @('.ini','.nss','.rtf','.txt','.bat','.log','.xml','.json','.csv','.md','.mdl','.mdx','.tpc','.tga','.mp3','.lip','.mod','.pwk','.wok','.DS_Store','.txi','.lyt','.vis')
$converted = 0; $skipped = 0; $failed = 0; $errorList = @()
$dirs = Get-ChildItem $fixBase -Directory | Select-Object -ExpandProperty Name
foreach ($d in $dirs) {
    $tpDir = [System.IO.Path]::Combine($fixBase, $d, "tslpatchdata")
    if (-not (Test-Path $tpDir)) { continue }
    $files = Get-ChildItem $tpDir -File -Recurse | Where-Object { $_.Extension -notin @('.ini','.nss','.rtf','.txt','.bat','.log','.xml','.json','.csv','.md') }
    if (-not $files -or $files.Count -eq 0) { continue }
    foreach ($f in $files) {
        $ext = $f.Extension.ToLower()
        $relPath = $f.FullName.Substring($tpDir.Length + 1)
        if ($ext -in $skipExts) { $skipped++; continue }
        $inputPath = $f.FullName
        $cmd = $null; $outSuffix = $null
        if ($ext -eq '.2da') { $cmd = "2da2csv"; $outSuffix = ".csv" }
        elseif ($ext -eq '.tlk') { $cmd = "tlk2xml"; $outSuffix = ".xml" }
        elseif ($ext -eq '.ssf') { $cmd = "ssf2xml"; $outSuffix = ".xml" }
        elseif ($ext -in $gffExts) { $cmd = "gff2xml"; $outSuffix = ".xml" }
        elseif ($ext -eq '.ncs') { $cmd = "disassemble"; $outSuffix = ".disasm.txt" }
        else { $skipped++; continue }
        $outFile = [System.IO.Path]::Combine($outBase, $d, $relPath + $outSuffix)
        $outFileDir = [System.IO.Path]::GetDirectoryName($outFile)
        if (-not (Test-Path $outFileDir)) { New-Item -ItemType Directory -Path $outFileDir -Force | Out-Null }
        try {
            $result = & python -m pykotor $cmd $inputPath --output $outFile 2>&1
            if ($LASTEXITCODE -eq 0) { $converted++; Write-Host "  OK: $d/$relPath -> $cmd" }
            else { $failed++; $errorList += "$d/$relPath : $result"; Write-Host "  FAIL: $d/$relPath" }
        } catch { $failed++; $errorList += "$d/$relPath : $_"; Write-Host "  ERROR: $d/$relPath" }
    }
}
Write-Host "`n=== CONVERSION SUMMARY ===`nConverted: $converted`nSkipped: $skipped`nFailed: $failed"
if ($errorList.Count -gt 0) { Write-Host "`n=== ERRORS ==="; foreach ($e in $errorList) { Write-Host "  $e" } }
