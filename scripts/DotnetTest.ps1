#Requires -Version 5.1
<#
.SYNOPSIS
    Runs dotnet test under a wall-clock timeout and kills the process tree when time expires.

.DESCRIPTION
    **Agents and CI must use this script (or dotnet-test.sh on Unix), not bare `dotnet test`.** Run it once per check; if it exits 124 (timeout) or does not finish within one invocation, stop and find the bottleneck — do not poll the same run for hours.

    Use this instead of invoking dotnet test directly so hung test runs cannot block agents or CI shells indefinitely.

    Implemented with $args (no param block) so the first token is never bound to a [int] timeout by mistake.

    Default timeout order:
    1) -TimeoutSeconds N (if present at the start of the argument list; parsed and stripped)
    2) Environment variable DOTNET_TEST_TIMEOUT_SECONDS (if set to a positive integer)
    3) 600 seconds (10 minutes)

    Values above 600 are clamped to 600 — **never** exceed 10 minutes total wall clock for `dotnet test` (no env override can raise the cap).

    If the timeout elapses, the wrapper terminates the dotnet process tree and exits with code 124 (GNU timeout convention).
    Treat exit 124 as mandatory bottleneck work: profile, trace, and optimize until the suite finishes **well under** 10 minutes; do not satisfy timeouts by disabling, skipping, or shrinking coverage as the primary fix.

.EXAMPLE
    .\scripts\DotnetTest.ps1 KPatcher.sln -c Debug

.EXAMPLE
    pwsh -File .\scripts\DotnetTest.ps1 -TimeoutSeconds 600 KPatcher.sln -c Debug

.EXAMPLE
    $env:DOTNET_TEST_TIMEOUT_SECONDS = '600'; .\scripts\DotnetTest.ps1 KPatcher.sln -c Debug
#>
$ErrorActionPreference = 'Stop'
$script:DotnetTestMaxWallClockSeconds = 600

function Resolve-TimeoutSeconds {
    param([int] $ExplicitFromSwitch)
    $resolved = 0
    if ($ExplicitFromSwitch -gt 0) {
        $resolved = $ExplicitFromSwitch
    } else {
        $ev = $env:DOTNET_TEST_TIMEOUT_SECONDS
        if (-not [string]::IsNullOrWhiteSpace($ev)) {
            $parsed = 0
            if ([int]::TryParse($ev, [System.Globalization.NumberStyles]::Integer, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$parsed) -and $parsed -gt 0) {
                $resolved = $parsed
            }
        }
    }
    if ($resolved -le 0) {
        $resolved = $script:DotnetTestMaxWallClockSeconds
    }
    if ($resolved -gt $script:DotnetTestMaxWallClockSeconds) {
        return $script:DotnetTestMaxWallClockSeconds
    }
    return $resolved
}

function Split-TimeoutSwitchFromArgs {
    param([string[]] $Raw)
    $explicitTimeout = 0
    $out = [System.Collections.Generic.List[string]]::new()
    for ($i = 0; $i -lt $Raw.Count; $i++) {
        $t = $Raw[$i]
        if (($t -ceq '-TimeoutSeconds' -or $t -ceq '/TimeoutSeconds') -and ($i + 1) -lt $Raw.Count) {
            $next = $Raw[$i + 1]
            $parsed = 0
            if ([int]::TryParse($next, [System.Globalization.NumberStyles]::Integer, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$parsed) -and $parsed -gt 0) {
                $explicitTimeout = $parsed
                $i++
                continue
            }
        }
        if ($t -eq '--' -and $out.Count -eq 0 -and $i -eq 0) {
            continue
        }
        $out.Add($t) | Out-Null
    }
    return [pscustomobject]@{
        Args    = @($out)
        Timeout = $explicitTimeout
    }
}

$raw = @(if ($null -ne $args -and $args.Count -gt 0) { $args } else { @() })
$split = Split-TimeoutSwitchFromArgs -Raw $raw
$DotnetTestArgs = $split.Args
$TimeoutSeconds = Resolve-TimeoutSeconds -ExplicitFromSwitch $split.Timeout
$timeoutMs = [long]$TimeoutSeconds * 1000L

if ($DotnetTestArgs.Count -eq 0) {
    if (Test-Path -LiteralPath (Join-Path (Get-Location) 'KPatcher.sln')) {
        $DotnetTestArgs = @('KPatcher.sln', '-c', 'Debug')
    } else {
        Write-Error @"
No arguments passed and KPatcher.sln not found in the current directory.

Usage:
  .\scripts\DotnetTest.ps1 [-TimeoutSeconds N] <arguments to dotnet test...>  (N capped at 600)

Examples:
  .\scripts\DotnetTest.ps1 KPatcher.sln -c Debug
  pwsh -File .\scripts\DotnetTest.ps1 -TimeoutSeconds 600 KPatcher.sln -c Debug
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
if (-not $finished) {
    $elapsedNote = "dotnet test exceeded ${TimeoutSeconds}s (max ${script:DotnetTestMaxWallClockSeconds}s; DOTNET_TEST_TIMEOUT_SECONDS / -TimeoutSeconds / default); terminating process tree (PID $($p.Id))."
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
