#Requires -Version 5.1
<#
.SYNOPSIS
    Runs dotnet test under a wall-clock timeout and kills the process tree when time expires.

.DESCRIPTION
    Use this instead of invoking dotnet test directly so hung test runs cannot block agents or CI shells indefinitely.

    Default timeout order:
    1) -TimeoutSeconds (if positive)
    2) Environment variable DOTNET_TEST_TIMEOUT_SECONDS (if set to a positive integer)
    3) 7200 seconds (2 hours)

    If the timeout elapses, the wrapper terminates the dotnet process tree and exits with code 124 (GNU timeout convention).

.PARAMETER TimeoutSeconds
    Wall-clock limit in seconds. Use 0 to take only env/default (7200).

.PARAMETER DotnetTestArgs
    All remaining arguments are passed to dotnet test after the literal subcommand test.

.EXAMPLE
    .\scripts\DotnetTest.ps1 KPatcher.sln -c Debug

.EXAMPLE
    .\scripts\DotnetTest.ps1 -TimeoutSeconds 3600 tests\KPatcher.Tests\KPatcher.Tests.csproj
#>
[CmdletBinding()]
param(
    [int] $TimeoutSeconds = 0,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $DotnetTestArgs = @()
)

$ErrorActionPreference = 'Stop'

function Resolve-TimeoutSeconds {
    param([int] $Explicit)
    if ($Explicit -gt 0) {
        return $Explicit
    }
    $ev = $env:DOTNET_TEST_TIMEOUT_SECONDS
    if (-not [string]::IsNullOrWhiteSpace($ev)) {
        $parsed = 0
        if ([int]::TryParse($ev, [System.Globalization.NumberStyles]::Integer, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$parsed) -and $parsed -gt 0) {
            return $parsed
        }
    }
    return 7200
}

$TimeoutSeconds = Resolve-TimeoutSeconds -Explicit $TimeoutSeconds
$timeoutMs = [long]$TimeoutSeconds * 1000L

if ($DotnetTestArgs.Count -eq 0) {
    if (Test-Path -LiteralPath (Join-Path (Get-Location) 'KPatcher.sln')) {
        $DotnetTestArgs = @('KPatcher.sln', '-c', 'Debug')
    } else {
        Write-Error @"
No arguments passed and KPatcher.sln not found in the current directory.

Usage:
  .\scripts\DotnetTest.ps1 [-TimeoutSeconds N] <arguments to dotnet test...>

Example:
  .\scripts\DotnetTest.ps1 KPatcher.sln -c Debug

Note: When calling via pwsh -File, do not insert -- before arguments (it is parsed by the host).
"@
        exit 2
    }
}

$argList = @('test') + $DotnetTestArgs
$p = Start-Process -FilePath 'dotnet' -ArgumentList $argList -NoNewWindow -PassThru -WorkingDirectory (Get-Location).Path
if ($null -eq $p) {
    Write-Error 'Failed to start dotnet.'
    exit 1
}

$finished = $p.WaitForExit([int][Math]::Min($timeoutMs, [int]::MaxValue))
# WaitForExit(int) is capped; for timeouts > ~24 days use loop — not needed for test runs.
if (-not $finished) {
    $elapsedNote = "dotnet test exceeded ${TimeoutSeconds}s (DOTNET_TEST_TIMEOUT_SECONDS / default); terminating process tree (PID $($p.Id))."
    Write-Warning $elapsedNote
    try {
        & taskkill.exe /PID $p.Id /T /F 2>$null | Out-Null
    } catch {
        # ignore
    }
    try {
        if (-not $p.HasExited) {
            Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
        }
    } catch {
        # ignore
    }
    exit 124
}

exit $p.ExitCode
