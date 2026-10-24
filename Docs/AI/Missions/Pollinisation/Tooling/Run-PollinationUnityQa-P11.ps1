param(
    [string]$ProjectPath = 'C:\BeeKingdom\DEV\BK-Studio\staging\pollinisation-alpha-20261021',
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.5.3f1\Editor\Unity.exe',
    [string]$QaDirectory = 'C:\BeeKingdom\DEV\BK-Studio\qa\pollinisation-p9-20261022',
    [ValidateSet('EditMode','PlayMode')]
    [string]$TestPlatform = 'EditMode',
    [string]$TestFilter = 'Pollination',
    [switch]$DryRun
)

# Fail closed: no main, no Unity instance already editing this worktree.
# UPM environment changes are PROCESS-LOCAL and restored after the test.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw 'Required Unity Editor executable is missing.'
}
$branch = (& git -C $ProjectPath rev-parse --abbrev-ref HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $branch -ne 'feature/pollinisation-alpha-20261021') {
    throw 'Only the isolated official pollination worktree is supported.'
}
New-Item -Path $QaDirectory -ItemType Directory -Force | Out-Null
$preflight = Join-Path $ProjectPath 'Docs\AI\Missions\Pollinisation\Tooling\verify_unity_asset_pairs.py'
$report = Join-Path $QaDirectory 'p11-qa-preflight.json'
& python -B $preflight --worktree $ProjectPath --json-report $report
if ($LASTEXITCODE -ne 0) {
    throw 'Asset and .meta integrity preflight failed. Unity not launched.'
}

$active = @(Get-CimInstance Win32_Process -Filter "name='Unity.exe'" | Where-Object {
    $_.CommandLine -like "*$ProjectPath*"
})
if ($active.Count -gt 0 -and -not $DryRun) {
    throw 'The target Unity worktree is already open. Unity not launched.'
}
if ($DryRun) {
    Write-Output "DRY_RUN=true; BRANCH=$branch; ASSETS=PASS"
    return
}

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
if ($TestFilter -notmatch '^[a-zA-Z0-9_.-]+$') {
    throw 'TestFilter must contain only safe filename/Unity filter characters.'
}
$xml = Join-Path $QaDirectory ('p11-qa-' + $TestPlatform + '-' + $TestFilter + '-' + $stamp + '.xml')
$log = Join-Path $QaDirectory ('p11-qa-' + $TestPlatform + '-' + $TestFilter + '-' + $stamp + '.log')
$upm = Join-Path $QaDirectory ('p11-qa-' + $TestPlatform + '-' + $TestFilter + '-' + $stamp + '-upm.log')
$arguments = @('-batchmode','-nographics','-projectPath',$ProjectPath,
    '-runTests','-testPlatform',$TestPlatform,'-testFilter',$TestFilter,
    '-testResults',$xml,'-logFile',$log,'-upmLogFile',$upm)

$common = [Environment]::GetFolderPath('CommonApplicationData')
if ([string]::IsNullOrWhiteSpace($common) -or
    -not (Test-Path -LiteralPath $common -PathType Container)) {
    throw 'Windows common application data path is unavailable.'
}
$names = @('PROGRAMDATA','ALLUSERSPROFILE','TMP','HOME')
$original = @{}
foreach ($name in $names) {
    $original[$name] = [Environment]::GetEnvironmentVariable($name,'Process')
}
try {
    [Environment]::SetEnvironmentVariable('PROGRAMDATA',$common,'Process')
    [Environment]::SetEnvironmentVariable('ALLUSERSPROFILE',$common,'Process')
    $tmp = [Environment]::GetEnvironmentVariable('TMP','User')
    if ([string]::IsNullOrWhiteSpace($tmp)) { $tmp = $env:TEMP }
    [Environment]::SetEnvironmentVariable('TMP',$tmp,'Process')
    [Environment]::SetEnvironmentVariable('HOME',$env:USERPROFILE,'Process')
    Write-Output ('UNITY_QA_START: '+$TestPlatform+'/'+$TestFilter)
    # Do NOT use -noUpm: it makes TMP/UI package references vanish.
    # Do NOT use -quit with -runTests: Unity 6.5 docs warn about early exit.
    $child = Start-Process -FilePath $UnityPath -ArgumentList $arguments -PassThru
    $deadline = (Get-Date).AddMinutes(45)
    $complete = $false
    $result = $null
    while ((Get-Date) -lt $deadline) {
        if (Test-Path -LiteralPath $xml -PathType Leaf) {
            try {
                [xml]$candidate = Get-Content -LiteralPath $xml -Raw
                if ($candidate.DocumentElement.Name -eq 'test-run' -and
                    $candidate.DocumentElement.GetAttribute('result')) {
                    $complete = $true
                    $result = $candidate
                    break
                }
            }
            catch {
                # Unity might still be writing the output XML.
            }
        }
        $child.Refresh()
        if ($child.HasExited) { break }
        Start-Sleep -Seconds 2
    }

    # A Unity batch-mode editor can remain alive after NUnit writes its
    # completed XML. Allow graceful cleanup; stop ONLY our own QA process if
    # it is still alive, never another project/editor/session.
    $child.Refresh()
    $forcedStop = $false
    if ($complete -and -not $child.HasExited) {
        for ($i=0; $i -lt 15; $i++) {
            Start-Sleep -Seconds 2
            $child.Refresh()
            if ($child.HasExited) { break }
        }
    }
    if (-not $child.HasExited) {
        $owned = Get-CimInstance Win32_Process -Filter ("ProcessId=" + $child.Id)
        if ($owned -and $owned.Name -eq 'Unity.exe' -and
            $owned.CommandLine -like '*-batchmode*' -and
            $owned.CommandLine -like "*$ProjectPath*") {
            Stop-Process -Id $child.Id -Force -ErrorAction Stop
            $forcedStop = $true
            Write-Output 'UNITY_QA_OWN_BATCH_STOPPED_AFTER_RESULT_OR_TIMEOUT=true'
        }
        else {
            throw 'Could not verify ownership of lingering Unity process. No process stopped.'
        }
    }

    if (-not $complete) {
        throw ('No completed Unity XML: ' + $xml + '. See ' + $log)
    }
    $testRun = $result.DocumentElement
    $status = $testRun.GetAttribute('result')
    $failed = $testRun.GetAttribute('failed')
    $passed = $testRun.GetAttribute('passed')
    $total = $testRun.GetAttribute('total')
    $skipped = $testRun.GetAttribute('skipped')
    Write-Output ('UNITY_QA_RESULTS=' + $status + ';TOTAL=' + $total +
        ';PASSED=' + $passed + ';FAILED=' + $failed + ';SKIPPED=' + $skipped)
    Write-Output ('UNITY_QA_XML=' + $xml)
    if ($status -ne 'Passed' -or $failed -ne '0') {
        throw 'Unity reported failed/inconclusive tests. Inspect XML.'
    }
    if (-not $forcedStop -and $child.ExitCode -ne 0) {
        throw 'Unity test process exited unsuccessfully despite XML.'
    }
}
finally {
    foreach ($name in $names) {
        [Environment]::SetEnvironmentVariable($name,$original[$name],'Process')
    }
}
