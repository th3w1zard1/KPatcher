param(
    [switch]$RequireGuiToolchain
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$vendorRoot = Join-Path $repoRoot 'vendor\TSLPatcher'
$sharedRoot = Join-Path $vendorRoot 'Shared'
$sharedUnits = @(
    'UTypes.pas',
    'UST_Common.pas',
    'UStrTok.pas',
    'UST_IniFile.pas',
    'UTLKFile.pas',
    'U2DAEdit.pas',
    'UGFFFile.pas',
    'UERFHandler.pas',
    'USSFFile.pas',
    'UTSLPatcher.pas'
)

Write-Host 'Verifying reverse-engineered TSLPatcher vendor tree...' -ForegroundColor Cyan
Write-Host "Vendor root: $vendorRoot"

if (-not (Test-Path $vendorRoot)) {
    throw "Vendor tree not found: $vendorRoot"
}

$stubMatches = Get-ChildItem -Path $vendorRoot -Recurse -Filter '*.pas' |
    Select-String -Pattern 'begin \{ 0x[0-9A-Fa-f]+ \} end;'

if ($stubMatches) {
    Write-Host ''
    Write-Host 'Empty reverse-engineering stubs remain:' -ForegroundColor Red
    $stubMatches | ForEach-Object {
        Write-Host ("  {0}:{1}" -f $_.Path, $_.LineNumber)
    }
    throw 'Stub verification failed.'
}

Write-Host 'No empty method stubs found.' -ForegroundColor Green

$fpc = Get-Command fpc.exe -ErrorAction SilentlyContinue
if (-not $fpc) {
    throw 'fpc.exe was not found on PATH. Install Free Pascal to run shared-unit verification.'
}

Write-Host ''
Write-Host ('Using FPC: ' + $fpc.Source) -ForegroundColor Cyan

Push-Location $sharedRoot
try {
    foreach ($unit in $sharedUnits) {
        Write-Host ''
        Write-Host ("Compiling shared unit: {0}" -f $unit) -ForegroundColor Yellow
        & $fpc.Source -Mdelphi -S2 -vw -Fu. -l $unit
        if ($LASTEXITCODE -ne 0) {
            throw "Shared unit compilation failed: $unit"
        }
    }
}
finally {
    Pop-Location
}

Write-Host ''
Write-Host 'Shared-unit verification passed.' -ForegroundColor Green

$guiProjects = @(
    (Join-Path $vendorRoot 'TSLPatcher\TSLPatcher.dpr'),
    (Join-Path $vendorRoot 'ChangeEdit\ChangeEdit.dpr')
)

$guiFailures = @()
foreach ($project in $guiProjects) {
    Write-Host ''
    Write-Host ("Probing GUI project: {0}" -f $project) -ForegroundColor Yellow
    $projectDir = Split-Path -Parent $project
    Push-Location $projectDir
    try {
        $output = & $fpc.Source -Mdelphi -S2 -vw -Fu..\Shared -l $project 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally {
        Pop-Location
    }

    if ($exitCode -eq 0) {
        Write-Host '  GUI project compiled successfully.' -ForegroundColor Green
        continue
    }

    $outputText = ($output | Out-String)
    if ($outputText -match 'Can''t find unit Forms used by') {
        Write-Host '  GUI toolchain not available locally (missing Forms unit).' -ForegroundColor DarkYellow
        if ($RequireGuiToolchain) {
            $guiFailures += $project
        }
        continue
    }

    Write-Host $outputText
    $guiFailures += $project
}

if ($guiFailures.Count -gt 0) {
    throw ('GUI project verification failed: ' + ($guiFailures -join ', '))
}

Write-Host ''
Write-Host 'TSLPatcher vendor verification complete.' -ForegroundColor Green