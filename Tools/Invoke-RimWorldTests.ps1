#Requires -Version 5.1
<#
.SYNOPSIS
    Builds the mod, runs its RimTest Redux suites inside RimWorld unattended, and prints one verdict.

.DESCRIPTION
    Replaces the manual loop: launch by hand, start a dev quicktest world by hand, open the RimTest
    window, click run, read tmp/TestCoverage.txt.

    The game is armed with -rbptest=<runId>. Without that argument the harness code inside
    RebalancePatchesTests.dll does nothing at all, so this script is the only thing that can make a
    RimWorld launch close itself.

      * Prefs.xml is seeded and restored. PrefsData.Apply() runs at frame 4 and overrides anything
        passed on the command line, and with runInBackground=False Unity stops calling Update the
        moment the window loses focus - an unattended run would freeze and only a timeout would
        notice. resetModsConfigOnCrash is turned off for the duration so a crash fails the run
        instead of possibly wiping a 581-mod list. Restoring all of it is in a finally block.

      * A stale build is its own verdict. The DLL's timestamp is recorded after the build and
        compared against the timestamp of the DLL the game reports having loaded. A run against a
        build made before the last edit has produced confident reports of failures that no longer
        existed. 

      * "Tests failed" and "the run never happened" are never conflated. Every exit path below names
        which one it was.

.PARAMETER KeepOpen
    Run everything except the quit. The script then waits for RimWorld to be closed by hand before restoring Prefs.xml.

.EXAMPLE
    pwsh -File Tools\Invoke-RimWorldTests.ps1
#>
[CmdletBinding()]
param(
    [string] $RimWorldDir = 'C:\Program Files (x86)\Steam\steamapps\common\RimWorld',
    [string] $SaveDataDir = (Join-Path $env:USERPROFILE 'AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios'),

    # Calibrated on four real runs: mod-init ~30s, defs-ready ~127s, menu-ready ~235s, map-ready
    # ~277s, quit ~289s - about five minutes end to end. Left generous rather than trimmed to the
    # observed times, since a slower machine or a larger modlist should wait, not report a false hang.
    [int]    $TimeoutMinutes = 20,
    [int]    $PhaseTimeoutMinutes = 8,

    # Forces a failure path so it can be exercised on purpose. 'fallback' drives the suites with the
    # backup runner instead of RimTest's; 'abort' quits before the map. Both should produce a
    # non-pass verdict - that is the point - so a run using this is not a regression signal.
    [ValidateSet('', 'fallback', 'abort', 'mapgen')]
    [string] $SelfTest = '',

    [switch] $KeepOpen,
    [switch] $SkipBuild
)

$ErrorActionPreference = 'Stop'

# Exit codes, so a caller can branch without parsing the text.
$EXIT = @{
    'pass'          = 0
    'tests-failed'  = 1
    'build-failed'  = 2
    'stale-build'   = 3
    'no-run'        = 4
    'launch-failed' = 4
    'mapgen-failed' = 6
    'hung'          = 5
}

function Write-Head([string] $text) {
    Write-Host ''
    Write-Host $text -ForegroundColor Cyan
}

function Write-Verdict([string] $verdict, [string] $detail) {
    $colour = 'Red'
    if ($verdict -eq 'pass') { $colour = 'Green' }
    Write-Host ''
    Write-Host "VERDICT: $verdict" -ForegroundColor $colour
    if ($detail) { Write-Host "  $detail" }
}

function Read-Heartbeat([string] $path) {
    if (-not (Test-Path -LiteralPath $path)) { return $null }
    try {
        # Opened share-read-write: the game rewrites this file while we poll it, and a sharing
        # violation here must read as "no news", never as a failure.
        $stream = [System.IO.File]::Open($path, 'Open', 'Read', 'ReadWrite')
        try {
            $reader = New-Object System.IO.StreamReader($stream)
            $line = $reader.ReadToEnd()
        }
        finally { $stream.Dispose() }
    }
    catch { return $null }

    $parts = ($line.Trim() -split '\s+')
    if ($parts.Count -lt 3) { return $null }
    return [pscustomobject]@{ runId = $parts[0]; phase = $parts[1]; utc = $parts[2] }
}

function Set-PrefValue([System.Xml.XmlDocument] $doc, [string] $name, [string] $value) {
    $node = $doc.PrefsData.SelectSingleNode($name)
    if ($null -eq $node) {
        $node = $doc.CreateElement($name)
        [void] $doc.PrefsData.AppendChild($node)
    }
    $node.InnerText = $value
}

function Get-PrefValue([string] $path, [string] $name) {
    try {
        $doc = New-Object System.Xml.XmlDocument
        $doc.Load($path)
        $node = $doc.PrefsData.SelectSingleNode($name)
        if ($null -eq $node) { return $null }
        return $node.InnerText
    }
    catch { return $null }
}

<#
Picks what to restore Prefs.xml from, and guards against the one failure that compounds.

Each run backs up Prefs.xml and restores it in a finally. If a run is killed hard enough that the
finally never executes - the terminal closed, the machine rebooted - the seeded file survives. The
next run would then back THAT up and faithfully restore it, permanently baking in windowed 720p and,
far worse, resetModsConfigOnCrash=False. That preference is off only for the duration of a run, on
purpose: leaving it off forever quietly removes the protection it was turned off to bound.

resetModsConfigOnCrash=False before we have touched anything is the tell, since nothing but this
script sets it. When that is seen, the freshly taken backup is not trustworthy, so an older one that
still reads True is preferred. If no clean backup survives, the caller is told and the value is
forced back to True on restore rather than left as found.
#>
function Resolve-PrefsRestoreSource([string] $prefsPath, [string] $freshBackup, [string] $workDir) {
    if ((Get-PrefValue $prefsPath 'resetModsConfigOnCrash') -ne 'False') {
        return [pscustomobject]@{ Path = $freshBackup; Contaminated = $false }
    }

    Write-Warning 'Prefs.xml already has resetModsConfigOnCrash=False, which only this script sets.'
    Write-Warning 'A previous run very likely failed to restore it. Looking for a clean backup...'

    $clean = Get-ChildItem -LiteralPath $workDir -Filter 'Prefs-backup-*.xml' -ErrorAction SilentlyContinue |
             Where-Object { $_.FullName -ne $freshBackup } |
             Sort-Object LastWriteTime -Descending |
             Where-Object { (Get-PrefValue $_.FullName 'resetModsConfigOnCrash') -eq 'True' } |
             Select-Object -First 1

    if ($clean) {
        Write-Host "  Using $($clean.Name) as the restore source instead." -ForegroundColor Yellow
        return [pscustomobject]@{ Path = $clean.FullName; Contaminated = $true }
    }

    Write-Warning '  No clean backup found. Restoring will force resetModsConfigOnCrash back to True.'
    return [pscustomobject]@{ Path = $freshBackup; Contaminated = $true }
}

# ---------------------------------------------------------------------------------------------
# Paths and prerequisites
# ---------------------------------------------------------------------------------------------

$repoRoot   = Split-Path -Parent $PSScriptRoot
$project    = Join-Path $repoRoot 'Source\RebalancePatches.Tests\RebalancePatches.Tests.csproj'
$testDll    = Join-Path $repoRoot 'Tests\Assemblies\RebalancePatchesTests.dll'
$workDir    = Join-Path $repoRoot '.utmp'
$tmpDir     = Join-Path $SaveDataDir 'RebalancePatches\tmp'
$prefsPath  = Join-Path $SaveDataDir 'Config\Prefs.xml'
$exePath    = Join-Path $RimWorldDir 'RimWorldWin64.exe'
$coveragePath  = Join-Path $tmpDir 'TestCoverage.txt'
$runJsonPath   = Join-Path $tmpDir 'TestRun.json'
$heartbeatPath = Join-Path $tmpDir 'heartbeat.txt'

if (-not (Test-Path -LiteralPath $exePath)) {
    Write-Verdict 'launch-failed' "RimWorld not found at $exePath. Pass -RimWorldDir."
    exit $EXIT['launch-failed']
}
if (-not (Test-Path -LiteralPath $prefsPath)) {
    Write-Verdict 'launch-failed' "Prefs.xml not found at $prefsPath. Pass -SaveDataDir."
    exit $EXIT['launch-failed']
}

# The modlist is almost entirely workshop items resolved through the Steam API. Without Steam the
# game shows a SteamClientMissing dialog and never reaches a map, and a workshop-less mod list is
# exactly the situation where a crash can do damage.
if (-not (Get-Process -Name 'steam' -ErrorAction SilentlyContinue)) {
    Write-Verdict 'launch-failed' 'Steam is not running. Start Steam first - the modlist is resolved through it.'
    exit $EXIT['launch-failed']
}
if (Get-Process -Name 'RimWorldWin64' -ErrorAction SilentlyContinue) {
    Write-Verdict 'launch-failed' 'RimWorld is already running. Close it first.'
    exit $EXIT['launch-failed']
}

[void] (New-Item -ItemType Directory -Force -Path $workDir)

# ---------------------------------------------------------------------------------------------
# Build
# ---------------------------------------------------------------------------------------------

if (-not $SkipBuild) {
    Write-Head 'Building...'
    & dotnet build $project -v minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Verdict 'build-failed' 'dotnet build failed; RimWorld was never launched.'
        exit $EXIT['build-failed']
    }
}

if (-not (Test-Path -LiteralPath $testDll)) {
    Write-Verdict 'build-failed' "$testDll does not exist."
    exit $EXIT['build-failed']
}
$builtStampUtc = (Get-Item -LiteralPath $testDll).LastWriteTimeUtc
Write-Host "Test assembly built $($builtStampUtc.ToString('o'))"

# ---------------------------------------------------------------------------------------------
# Run id and a clean slate
# ---------------------------------------------------------------------------------------------

# No '=' anywhere in this: GenCommandLine.TryGetCommandLineArg splits the argument on '=' and
# requires exactly two parts, so a run id containing one makes -rbptest invisible and the harness
# silently never arms.
$runId = Get-Date -Format 'yyyyMMdd-HHmmss'
$logPath = Join-Path $workDir "run-$runId.log"

[void] (New-Item -ItemType Directory -Force -Path $tmpDir)
foreach ($stale in @($coveragePath, $runJsonPath, $heartbeatPath)) {
    if (Test-Path -LiteralPath $stale) { Remove-Item -LiteralPath $stale -Force }
}

# ---------------------------------------------------------------------------------------------
# Seed preferences, launch, watch
# ---------------------------------------------------------------------------------------------

$prefsBackup = Join-Path $workDir "Prefs-backup-$runId.xml"
Copy-Item -LiteralPath $prefsPath -Destination $prefsBackup -Force
Write-Host "Prefs.xml backed up to $prefsBackup"

$restoreFrom = Resolve-PrefsRestoreSource $prefsPath $prefsBackup $workDir

$process    = $null
$lastPhase  = $null
$killedFor  = $null
$elapsed    = [TimeSpan]::Zero

try {
    $prefs = New-Object System.Xml.XmlDocument
    $prefs.Load($prefsPath)
    Set-PrefValue $prefs 'runInBackground' 'True'          # or the run freezes on focus loss
    Set-PrefValue $prefs 'fullscreen' 'False'
    Set-PrefValue $prefs 'screenWidth' '1280'
    Set-PrefValue $prefs 'screenHeight' '720'
    Set-PrefValue $prefs 'resetModsConfigOnCrash' 'False'  # a crash fails the run, it does not wipe the modlist
    $prefs.Save($prefsPath)

    # Deliberately NOT -quicktest. That skips the main menu, and mods initialise there: Cherry Picker
    # does its def removals in a MainMenuOnGUI postfix, so under -quicktest the first automated run
    # reported 23 failures for defs that had simply never been removed. The harness draws the menu,
    # then starts the same game itself. See HarnessMenuStarter.
    $gameArgs = @("-rbptest=$runId", '-logfile', $logPath)
    if ($KeepOpen) { $gameArgs += '-rbpkeepopen' }
    if ($SelfTest) {
        $gameArgs += "-rbpselftest=$SelfTest"
        Write-Warning "SELF-TEST '$SelfTest': this run deliberately takes a failure path. A non-pass verdict is the expected result."
    }

    Write-Head "Launching RimWorld (run $runId)..."
    Write-Host "  $exePath $($gameArgs -join ' ')"
    $process = Start-Process -FilePath $exePath -ArgumentList $gameArgs -WorkingDirectory $RimWorldDir -PassThru

    $startedAt       = Get-Date
    $lastPhaseChange = $startedAt
    $overallLimit    = New-TimeSpan -Minutes $TimeoutMinutes
    $phaseLimit      = New-TimeSpan -Minutes $PhaseTimeoutMinutes
    $reportSeen      = $false
    $reportSeenAt    = $null
    # Root.Shutdown() shuts Steam down and clears Unity's cache folder before quitting; two minutes
    # is generous for that and still bounded, so a hang at the very last step cannot wedge the script.
    $shutdownGrace   = New-TimeSpan -Minutes 2

    while ($true) {
        Start-Sleep -Seconds 2

        $beat = Read-Heartbeat $heartbeatPath
        if ($beat -and $beat.runId -eq $runId -and $beat.phase -ne $lastPhase) {
            $lastPhase = $beat.phase
            $lastPhaseChange = Get-Date
            Write-Host ("  [{0,6:n0}s] {1}" -f ((Get-Date) - $startedAt).TotalSeconds, $lastPhase)
        }

        # Once the report exists the run is over as far as this script is concerned; with -KeepOpen
        # the game deliberately stays up, so neither timeout may fire from here on.
        if (-not $reportSeen -and (Test-Path -LiteralPath $runJsonPath)) {
            $reportSeen = $true
            $reportSeenAt = Get-Date
            Write-Host '  result file written'
        }

        if ($process.HasExited) { break }

        $now = Get-Date
        if (-not $reportSeen) {
            if (($now - $startedAt) -gt $overallLimit) { $killedFor = 'overall'; break }
            if (($now - $lastPhaseChange) -gt $phaseLimit) { $killedFor = 'phase'; break }
        }
        elseif ($KeepOpen) {
            Write-Host '  -KeepOpen: waiting for you to close RimWorld before restoring Prefs.xml...'
            $process.WaitForExit()
            break
        }
        elseif (($now - $reportSeenAt) -gt $shutdownGrace) {
            $killedFor = 'shutdown'
            break
        }
    }

    $elapsed = (Get-Date) - $startedAt
}
finally {
    try {
        if ($process -and -not $process.HasExited) {
            Write-Host 'Stopping RimWorld...' -ForegroundColor Yellow
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
            [void] $process.WaitForExit(30000)
        }
    }
    catch { Write-Warning "Could not stop RimWorld: $($_.Exception.Message)" }

    # Last, and only once the process is gone: RimWorld rewrites Prefs.xml on shutdown, so restoring
    # while it is still alive would be undone. If this ever fails, the backup path is the recovery.
    try {
        Copy-Item -LiteralPath $restoreFrom.Path -Destination $prefsPath -Force
        # Belt and braces when the source itself was suspect: this preference is off only for the
        # duration of a run, and leaving it off is the one outcome worth extra code to prevent.
        if ($restoreFrom.Contaminated -and (Get-PrefValue $prefsPath 'resetModsConfigOnCrash') -ne 'True') {
            $doc = New-Object System.Xml.XmlDocument
            $doc.Load($prefsPath)
            Set-PrefValue $doc 'resetModsConfigOnCrash' 'True'
            $doc.Save($prefsPath)
            Write-Host 'Forced resetModsConfigOnCrash back to True.' -ForegroundColor Yellow
        }
        Write-Host "Prefs.xml restored from $(Split-Path -Leaf $restoreFrom.Path)."
    }
    catch {
        Write-Host "COULD NOT RESTORE Prefs.xml. Copy $($restoreFrom.Path) over $prefsPath by hand." -ForegroundColor Red
    }
}

# ---------------------------------------------------------------------------------------------
# Verdict
# ---------------------------------------------------------------------------------------------

Write-Head ("Run finished after {0:n0}s (last phase: {1})" -f $elapsed.TotalSeconds, $(if ($lastPhase) { $lastPhase } else { 'none' }))
Write-Host "  player log:     $logPath"
Write-Host "  coverage:       $coveragePath"
Write-Host "  machine result: $runJsonPath"

if ($killedFor -eq 'shutdown') {
    # The results are on disk and valid; only the quit misbehaved, so this is a note, not a verdict.
    Write-Host 'NOTE: the game did not close itself after writing its result and was stopped.' -ForegroundColor Yellow
}
elseif ($killedFor) {
    $phaseName = if ($lastPhase) { $lastPhase } else { 'no-heartbeat' }
    $why = if ($killedFor -eq 'overall') { "overall timeout of $TimeoutMinutes min" } else { "phase timeout of $PhaseTimeoutMinutes min" }
    $extra = ''
    if ($phaseName -eq 'no-heartbeat') {
        $extra = ' The harness never armed - check that -rbptest reached the game and that the Tests mod is active.'
    }
    elseif ($phaseName -eq 'defs-ready') {
        # Worldgen and mapgen sit between defs-ready and map-ready, and their exception handler drops
        # the player back at the menu rather than quitting - which looks exactly like a hang here.
        $extra = ' Everything after this phase is worldgen and mapgen; if either threw, the game is sitting at the main menu.'
    }
    elseif ($phaseName -eq 'tests-running') {
        $extra = ' The suites themselves stalled.'
    }
    Write-Verdict "hung($phaseName)" "Killed after the $why.$extra The log was kept."
    exit $EXIT['hung']
}

if (-not (Test-Path -LiteralPath $runJsonPath)) {
    if (-not $lastPhase) {
        Write-Verdict 'launch-failed' 'The game exited without ever writing a heartbeat.'
        if (Test-Path -LiteralPath $logPath) {
            Get-Content -LiteralPath $logPath -Tail 40
        }
        else {
            # -logfile is a Unity player argument, assumed rather than verified for this build. If it
            # was not honoured the log went to the usual rotating file instead.
            Write-Host "  No log at $logPath - see $(Join-Path $SaveDataDir 'Player.log') instead."
        }
        exit $EXIT['launch-failed']
    }
    # menu-ready means the game was started and worldgen/mapgen is where it died; the two earlier
    # phases mean it never got that far. Both end at no map, but they point at different halves of
    # the run, so they are not collapsed into one message.
    if ($lastPhase -eq 'mod-init' -or $lastPhase -eq 'defs-ready') {
        Write-Verdict 'mapgen-failed' "The game loaded but never reached the main menu (stuck at '$lastPhase'). Check the log."
        exit $EXIT['mapgen-failed']
    }
    if ($lastPhase -eq 'menu-ready') {
        Write-Verdict 'mapgen-failed' 'The game started but worldgen or mapgen never produced a map. Check the log.'
        exit $EXIT['mapgen-failed']
    }
    Write-Verdict 'no-run' "The game reached '$lastPhase' and exited without writing a result."
    exit $EXIT['no-run']
}

$report = Get-Content -LiteralPath $runJsonPath -Raw -Encoding UTF8 | ConvertFrom-Json

if ($report.runId -ne $runId) {
    Write-Verdict 'no-run' "TestRun.json carries run id '$($report.runId)', not '$runId'. Numbers from it are not this run's."
    exit $EXIT['no-run']
}

# Staleness first, before a single number is read out loud.
$loadedStampText = $report.assembly.lastWriteUtc
if (-not $loadedStampText) {
    Write-Host 'WARNING: the game could not report which assembly file it loaded; staleness is unverified.' -ForegroundColor Yellow
}
else {
    $loadedStampUtc = [datetime]::Parse($loadedStampText, [cultureinfo]::InvariantCulture,
        [System.Globalization.DateTimeStyles]::RoundtripKind).ToUniversalTime()
    # Two seconds of slack, not zero: both sides read the same file through the same junction, but a
    # filesystem timestamp resolution difference must not be reported as a stale build.
    if ([math]::Abs(($loadedStampUtc - $builtStampUtc).TotalSeconds) -gt 2) {
        Write-Verdict 'stale-build' "The game ran an assembly built $($loadedStampUtc.ToString('o')), not the one built $($builtStampUtc.ToString('o')). No test result from this run means anything."
        Write-Host "  loaded from: $($report.assembly.path)"
        exit $EXIT['stale-build']
    }
}

if ($report.outcome -ne 'completed') {
    # Worldgen/mapgen gets its own verdict rather than being folded into no-run. Both mean the suites
    # did not run, but this one names a cause and points at a different half of the system - and it is
    # the most likely genuine failure of an unattended run, since nobody is watching to dismiss the
    # error dialog the game would otherwise sit on.
    if ($report.outcome -eq 'mapgen-failed') {
        Write-Verdict 'mapgen-failed' 'World or map generation threw; the suites never ran. The exception is in the errors below and in the log.'
        if ($report.errors) {
            Write-Head 'Errors captured before it stopped:'
            $report.errors | Select-Object -First 20 | ForEach-Object { Write-Host "  [$($_.level) x$($_.count)] $($_.text)" }
        }
        exit $EXIT['mapgen-failed']
    }
    Write-Verdict 'no-run' "The run reported outcome '$($report.outcome)' at phase '$($report.phase)' - the suites did not finish."
    if ($report.errors) {
        Write-Head 'Errors captured before it stopped:'
        $report.errors | Select-Object -First 20 | ForEach-Object { Write-Host "  [$($_.level) x$($_.count)] $($_.text)" }
    }
    exit $EXIT['no-run']
}

if ($report.runner -eq 'fallback') {
    Write-Host ''
    Write-Host '*** RimTest Redux''s API has moved: the suites ran on our backup runner. ***' -ForegroundColor Yellow
    Write-Host "    $($report.runnerNote)" -ForegroundColor Yellow
    Write-Host '    Results below are real, but fix the reflection target before trusting the next run.' -ForegroundColor Yellow
}

$counts = $report.counts
Write-Head 'Coverage'
Write-Host ("  {0} tests seen, {1} FAILED, {2} ran without asserting anything" -f $counts.seen, $counts.failed, $counts.ranWithoutAsserting)
Write-Host ("  {0} of {1} toggles exercised; {2} skipped (toggle off), {3} skipped (mod absent), {4} with no test reached" -f `
    $counts.ran, $counts.registeredToggles, $counts.skippedToggleOff, $counts.skippedModAbsent, $counts.noTestReached)
Write-Host ("  runner: {0} ({1} suites, {2} tests found); RimTest's own verdict: {3}" -f `
    $report.runner, $counts.suitesFound, $counts.testsFound, $(if ($report.runnerStatus) { $report.runnerStatus } else { 'unknown' }))

if ($report.lists.toggleOff.Count -gt 0) {
    Write-Head 'NOT TESTED (mods present, toggle off)'
    $report.lists.toggleOff | ForEach-Object { Write-Host "  $_" }
}
if ($report.lists.ranWithoutAsserting.Count -gt 0) {
    Write-Head 'RAN BUT ASSERTED NOTHING (green and worthless)'
    $report.lists.ranWithoutAsserting | ForEach-Object { Write-Host "  $_" }
}
if ($report.lists.unsurfaced.Count -gt 0) {
    Write-Head 'FAILURES NOT SURFACED (green in the runner, broken in fact)'
    $report.lists.unsurfaced | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
}

$errorEntries = @($report.errors | Where-Object { $_.level -ne 'warning' })
if ($errorEntries.Count -gt 0) {
    Write-Head "Errors logged during the run ($($errorEntries.Count) distinct)"
    $errorEntries | Select-Object -First 20 | ForEach-Object { Write-Host "  [$($_.level) x$($_.count)] $($_.text)" }
}

if ($counts.failed -gt 0) {
    Write-Head 'Failures'
    foreach ($failure in $report.failures) {
        Write-Host "  $($failure.test)" -ForegroundColor Red
        $failure.messages | ForEach-Object { Write-Host "      - $_" }
        $failure.notes | ForEach-Object { Write-Host "      note: $_" }
    }
    Write-Verdict 'tests-failed' "$($counts.failed) test(s) failed. Full detail in $coveragePath."
    exit $EXIT['tests-failed']
}

# RimTest's own tally disagreeing with ours means a test failed somewhere our assertion helpers never
# saw - an ordinary exception rather than a Check failure - and reporting a pass would be the exact
# false signal this harness exists to remove.
if ($report.runnerStatus -eq 'ERROR') {
    Write-Verdict 'tests-failed' "RimTest reports ERROR for our assembly while TestCoverage recorded no failures - a test threw outside the assertion helpers. Check $coveragePath and the log."
    exit $EXIT['tests-failed']
}

Write-Verdict 'pass' "All $($counts.seen) tests that ran passed. Skipped work is listed above."
exit $EXIT['pass']
