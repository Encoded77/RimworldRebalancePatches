#Requires -Version 5.1
<#
.SYNOPSIS
    Stages a clean, Workshop-ready copy of the mod and points the Steam Mods junction at it.

.DESCRIPTION
    The Steam entry Mods\RimworldRebalancePatches is a junction to the dev repo, so RimWorld's
    uploader (which has no .gitignore awareness) sweeps the whole tree - .git, .utmp test logs,
    Source/Analysis build output, and Local\Plan.md provenance - into the Workshop submission. That
    both bloats the item and can make Steam return "OnItemSubmitted failure. Result: Fail".

    This script copies only the shippable subset into a clean sibling folder and repoints the
    junction there for the upload. Publish flow:

        Tools\Stage-Publish.ps1            # build, stage clean copy, point junction at it
        # -> in RimWorld's mod list, Upload to Steam Workshop
        Tools\Stage-Publish.ps1 -Restore   # point the junction back at the dev repo for testing

    Only the junction target changes; the dev repo is never modified or deleted.

.PARAMETER Restore
    Skip staging and point the Steam Mods junction back at the dev repo.

.PARAMETER NoBuild
    Skip the Release build of the shippable DLL before staging (use the DLL already in 1.6\Assemblies).
#>
[CmdletBinding()]
param(
    [switch] $Restore,
    [switch] $NoBuild
)

$ErrorActionPreference = 'Stop'

$DevRoot     = 'C:\Code\RimworldRebalancePatches'
$PublishRoot = 'C:\Code\RimworldRebalancePatches-publish'
$LinkPath    = 'C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\RimworldRebalancePatches'

# Whitelist of what actually ships. Everything else (.git, .utmp, Source, Analysis, Local, Tools,
# Tests, .gitignore, .claude, .vscode) is dev-only and deliberately excluded.
$ShipDirs  = @('1.6', 'About', 'Docs')
$ShipFiles = @('LoadFolders.xml', 'LICENSE', 'README.md', 'CHANGELOG.md')

function Repoint-Junction {
    param([string] $Target)

    if (-not (Test-Path -LiteralPath $Target)) {
        throw "Junction target does not exist: $Target"
    }
    if (Test-Path -LiteralPath $LinkPath) {
        $item = Get-Item -LiteralPath $LinkPath -Force
        if ($item.LinkType -ne 'Junction') {
            throw "Refusing to touch '$LinkPath' - it is a real directory, not a junction. Move it aside by hand first."
        }
        # Delete the reparse point only (non-recursive) so the current target's contents are untouched.
        [System.IO.Directory]::Delete($LinkPath, $false)
    }
    New-Item -ItemType Junction -Path $LinkPath -Target $Target | Out-Null
    Write-Host "Junction: $LinkPath -> $Target" -ForegroundColor Green
}

if ($Restore) {
    Repoint-Junction -Target $DevRoot
    Write-Host 'Restored. The game and test harness now load the live dev repo.' -ForegroundColor Cyan
    return
}

# --- Stage ---

if (-not $NoBuild) {
    Write-Host 'Building shippable DLL (Release)...'
    & dotnet build (Join-Path $DevRoot 'Source\RebalancePatches\RebalancePatches.csproj') -c Release
    if ($LASTEXITCODE -ne 0) { throw "Build failed (exit $LASTEXITCODE) - not staging a stale DLL." }
}

$dll = Join-Path $DevRoot '1.6\Assemblies\RebalancePatches.dll'
if (-not (Test-Path -LiteralPath $dll)) {
    throw "Shippable DLL missing: $dll - build first (drop -NoBuild)."
}

# Safety: never let the publish path collapse onto the dev repo.
if ($PublishRoot -eq $DevRoot -or $PublishRoot -notlike '*-publish') {
    throw "Unsafe publish path: $PublishRoot"
}
if ((Test-Path -LiteralPath $PublishRoot) -and (Get-Item -LiteralPath $PublishRoot -Force).LinkType) {
    throw "Refusing to clean '$PublishRoot' - it is a link, not a plain folder."
}

Write-Host "Staging clean copy -> $PublishRoot"
if (Test-Path -LiteralPath $PublishRoot) { Remove-Item -LiteralPath $PublishRoot -Recurse -Force }
New-Item -ItemType Directory -Path $PublishRoot | Out-Null

foreach ($d in $ShipDirs) {
    $src = Join-Path $DevRoot $d
    if (-not (Test-Path -LiteralPath $src)) { Write-Warning "skip missing dir: $d"; continue }
    # /MIR mirrors; /XF *.pdb keeps debug symbols out of Assemblies. robocopy exit < 8 is success.
    & robocopy $src (Join-Path $PublishRoot $d) /MIR /XF *.pdb /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy failed for '$d' (exit $LASTEXITCODE)." }
}
foreach ($f in $ShipFiles) {
    $src = Join-Path $DevRoot $f
    if (Test-Path -LiteralPath $src) { Copy-Item -LiteralPath $src -Destination $PublishRoot }
    else { Write-Warning "skip missing file: $f" }
}

# Assert nothing dev-only leaked into the staged copy.
$forbidden = @('.git', '.utmp', 'Source', 'Analysis', 'Local', 'Tools', 'Tests', '.gitignore', '.claude', '.vscode')
$leaked = Get-ChildItem -LiteralPath $PublishRoot -Force | Where-Object { $forbidden -contains $_.Name }
if ($leaked) { throw "Dev-only content leaked into staging: $($leaked.Name -join ', ')" }
if (Get-ChildItem -Recurse -Force -LiteralPath $PublishRoot -Filter *.pdb) { throw 'A .pdb leaked into staging.' }

$files = Get-ChildItem -Recurse -Force -File -LiteralPath $PublishRoot
$mb = [math]::Round(($files | Measure-Object Length -Sum).Sum / 1MB, 1)
Write-Host ("Staged {0} files, {1} MB." -f $files.Count, $mb) -ForegroundColor Green

Repoint-Junction -Target $PublishRoot

Write-Host ''
Write-Host 'Next:' -ForegroundColor Cyan
Write-Host '  1. In RimWorld''s mod list, select Encoded''s Rebalance Patches and Upload to Steam Workshop.'
Write-Host '  2. When the upload succeeds, run:  Tools\Stage-Publish.ps1 -Restore'
Write-Host '     to point the junction back at the dev repo before you next build or test.'
