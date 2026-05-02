# Maintainer helper: extract a packaged mod's tslpatchdata tree into a neutral exhaustive fixture payload.
# Copies full mod-side bytes (except *.exe) so tests do not rely on retail game installs at runtime.
param(
    [Parameter(Mandatory = $true)]
    [string] $ZipPath,

    [Parameter(Mandatory = $true)]
    [string] $NeutralPayloadName,

    [string] $TslpatchdataSubpath = 'tslpatchdata',

    [string] $DestinationRoot = (Join-Path $PSScriptRoot '..\tests\KPatcher.Tests\test_files\exhaustive_pattern_inlines'),

    [switch] $CleanDestination,

    [switch] $ScrubTextBranding
)

$ErrorActionPreference = 'Stop'

function ConvertTo-NormalizedRelativePath {
    param([Parameter(Mandatory = $true)][string] $PathValue)

    return $PathValue.Replace('\', '/').Trim('/')
}

function Find-TslpatchdataDirectory {
    param(
        [Parameter(Mandatory = $true)][string] $Root,
        [Parameter(Mandatory = $true)][string] $RelativeSubpath
    )

    $normalized = ConvertTo-NormalizedRelativePath -PathValue $RelativeSubpath
    if ([string]::IsNullOrWhiteSpace($normalized)) {
        throw 'TslpatchdataSubpath cannot be blank.'
    }

    $candidates = Get-ChildItem -Path $Root -Recurse -Directory |
        ForEach-Object {
            $relative = [System.IO.Path]::GetRelativePath($Root, $_.FullName)
            $normalizedRelative = ConvertTo-NormalizedRelativePath -PathValue $relative

            if ($normalizedRelative.Equals($normalized, [StringComparison]::OrdinalIgnoreCase) -or
                $normalizedRelative.EndsWith("/$normalized", [StringComparison]::OrdinalIgnoreCase)) {
                [PSCustomObject]@{
                    Directory = $_.FullName
                    Relative = $normalizedRelative
                    Depth = ($normalizedRelative.Split('/').Count)
                }
            }
        } |
        Sort-Object -Property Depth, Relative

    if (-not $candidates) {
        throw "Could not find '$RelativeSubpath' inside extracted archive tree."
    }

    return $candidates[0].Directory
}

if (-not (Test-Path -LiteralPath $ZipPath -PathType Leaf)) {
    throw "Zip archive not found: $ZipPath"
}

$resolvedZipPath = (Resolve-Path -LiteralPath $ZipPath).Path
$resolvedDestinationRootPath = Resolve-Path -LiteralPath $DestinationRoot -ErrorAction SilentlyContinue
if ($null -ne $resolvedDestinationRootPath) {
    $resolvedDestinationRoot = $resolvedDestinationRootPath.Path
}
else {
    $resolvedDestinationRoot = [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $DestinationRoot))
}

if (-not (Test-Path -LiteralPath $resolvedDestinationRoot)) {
    New-Item -ItemType Directory -Path $resolvedDestinationRoot -Force | Out-Null
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("kp_exhaustive_extract_" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null

try {
    tar -xf $resolvedZipPath -C $tempRoot
    if ($LASTEXITCODE -ne 0) {
        throw "tar extraction failed with exit code $LASTEXITCODE for '$resolvedZipPath'."
    }

    $sourceTslpatchdata = Find-TslpatchdataDirectory -Root $tempRoot -RelativeSubpath $TslpatchdataSubpath

    $payloadRoot = Join-Path $resolvedDestinationRoot $NeutralPayloadName
    $destinationTslpatchdata = Join-Path $payloadRoot 'tslpatchdata'

    if ($CleanDestination -and (Test-Path -LiteralPath $destinationTslpatchdata)) {
        Remove-Item -LiteralPath $destinationTslpatchdata -Recurse -Force
    }

    New-Item -ItemType Directory -Path $destinationTslpatchdata -Force | Out-Null
    Get-ChildItem -LiteralPath $sourceTslpatchdata -Force |
        ForEach-Object {
            Copy-Item -LiteralPath $_.FullName -Destination $destinationTslpatchdata -Recurse -Force
        }

    $removedExeCount = 0
    Get-ChildItem -Path $destinationTslpatchdata -Recurse -File |
        Where-Object { $_.Extension.Equals('.exe', [StringComparison]::OrdinalIgnoreCase) } |
        ForEach-Object {
            Remove-Item -LiteralPath $_.FullName -Force
            $removedExeCount++
        }

    $scrubbedFileCount = 0
    if ($ScrubTextBranding) {
        $textExtensions = @('.ini', '.rtf', '.txt', '.nss', '.json', '.cfg', '.md')
        $replacements = @(
            @{ Pattern = '(?i)https?://(?:www\.)?deadlystream\.com\S*'; Replacement = 'https://example.invalid/mod-source' },
            @{ Pattern = '(?i)https?://(?:www\.)?dropbox\.com\S*'; Replacement = 'https://example.invalid/file-host' },
            @{ Pattern = '(?i)deadlystream\.com'; Replacement = 'example.invalid' },
            @{ Pattern = '(?i)dropbox\.com'; Replacement = 'example.invalid' }
        )

        Get-ChildItem -Path $destinationTslpatchdata -Recurse -File |
            Where-Object { $textExtensions -contains $_.Extension.ToLowerInvariant() } |
            ForEach-Object {
                $content = Get-Content -LiteralPath $_.FullName -Raw -ErrorAction SilentlyContinue
                if ($null -eq $content) {
                    return
                }

                $updated = $content
                foreach ($item in $replacements) {
                    $updated = [regex]::Replace($updated, $item.Pattern, $item.Replacement)
                }

                if (-not $updated.Equals($content, [StringComparison]::Ordinal)) {
                    Set-Content -LiteralPath $_.FullName -Value $updated -NoNewline -Encoding UTF8
                    $scrubbedFileCount++
                }
            }
    }

    $copiedFileCount = (Get-ChildItem -Path $destinationTslpatchdata -Recurse -File).Count
    Write-Host "Extracted: $resolvedZipPath"
    Write-Host "Source tslpatchdata: $sourceTslpatchdata"
    Write-Host "Destination: $destinationTslpatchdata"
    Write-Host "Copied files: $copiedFileCount"
    Write-Host "Removed .exe files: $removedExeCount"
    Write-Host "Scrubbed text files: $scrubbedFileCount"
}
finally {
    if (Test-Path -LiteralPath $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}
