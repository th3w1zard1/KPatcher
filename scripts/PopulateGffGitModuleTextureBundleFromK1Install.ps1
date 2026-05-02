# Maintainer helper: copy the K1 module referenced by gff_git_module_texture_bundle into the exhaustive fixture.
# Override/mesh/texture rows in changes.ini must still be copied from the
# packaged mod into tslpatchdata/Override/ separately (too many names to hardcode here).
param(
    [Parameter(Mandatory = $true)]
    [string] $K1GameRoot
)

$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot

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

$payloadRoot = Join-Path $PSScriptRoot '..\tests\KPatcher.Tests\test_files\exhaustive_pattern_inlines\gff_git_module_texture_bundle\tslpatchdata'
$srcModules = Resolve-K1ModulesPath -GameRoot $K1GameRoot
$modName = 'tar_m02ac.mod'

$src = Join-Path $srcModules $modName

New-Item -ItemType Directory -Force -Path $payloadRoot | Out-Null
$dest = Join-Path $payloadRoot $modName
if (Test-Path -LiteralPath $src) {
    Copy-Item -LiteralPath $src -Destination $dest -Force
}
elseif (-not (Test-Path -LiteralPath $dest -PathType Leaf)) {
    throw "Missing packaged module and no bootstrap fallback found: $dest"
}
else {
    Write-Host "Keeping packaged module bytes for $modName"
}

& (Join-Path $here 'PopulateExhaustiveInlineVanillaSourcesFromK1Install.ps1') -K1GameRoot $K1GameRoot -IniPaths @((Join-Path $payloadRoot 'changes.ini'))

Write-Host "Copied $modName to $payloadRoot"
$overrideFiles = @(
    'm02ac_02h.mdl', 'm02ac_02h.mdx', 'm02ac_02h.wok',
    'm02ac_02g.mdl', 'm02ac_02g.mdx', 'm02ac_02g.wok',
    'DP_CM_TnkGlass.tpc', 'DP_TarTnkGlass.tpc', 'DPFBCrispyB02.tpc', 'DPFBCrispyC02.tpc',
    'DPFHCrispyB02.tpc', 'DPFHCrispyC02.tpc', 'DPMBCrispyA02.tpc', 'DPMBCrispyB02.tpc',
    'DPMBCrispyC02.tpc', 'DPMHCrispyA02.tpc', 'DPMHCrispyA03.tpc', 'DPMHCrispyB02.tpc',
    'DPMHCrispyC02.tpc', 'DPMHCrispyC03.tpc'
)
Write-Host 'Copy these from the packaged mod into tests/.../gff_git_module_texture_bundle/tslpatchdata/Override/ :'
Write-Host ($overrideFiles -join ', ')
