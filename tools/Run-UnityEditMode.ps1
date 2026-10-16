[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$TestFilter,
    [string]$ProjectPath = (Split-Path -Parent $PSScriptRoot),
    [string]$UnityPath
)

$ErrorActionPreference = 'Stop'
$ProjectPath = (Resolve-Path -LiteralPath $ProjectPath).Path
if (!$UnityPath) {
    $version = (Select-String -LiteralPath "$ProjectPath/ProjectSettings/ProjectVersion.txt" `
        -Pattern '^m_EditorVersion: (.+)$').Matches.Groups[1].Value
    $UnityPath = "$env:ProgramFiles/Unity/Hub/Editor/$version/Editor/Unity.exe"
}
if (!(Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity introuvable : $UnityPath"
}

$outputPath = Join-Path $ProjectPath ("Logs/EditMode-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $outputPath | Out-Null
$resultsPath = Join-Path $outputPath 'results.xml'
$savedEnvironment = @{}
try {
    # UPM 9.26.1 needs both variables: configuration startup, then package resolution.
    # Restore only absent values, for this process and its children; no registry changes.
    foreach ($name in @('ProgramData', 'ALLUSERSPROFILE')) {
        $value = [Environment]::GetEnvironmentVariable($name, 'Process')
        if ([string]::IsNullOrEmpty($value)) {
            $savedEnvironment[$name] = $value
            [Environment]::SetEnvironmentVariable($name,
                [Environment]::GetFolderPath('CommonApplicationData'), 'Process')
        }
    }
    $arguments = @('-batchmode', '-nographics', '-projectPath', $ProjectPath,
        '-runTests', '-testPlatform', 'EditMode', '-testFilter', $TestFilter,
        '-testResults', $resultsPath, '-logFile', "$outputPath/editor.log",
        '-upmLogFile', "$outputPath/upm.log")
    if ($arguments | Where-Object { $_.Contains('"') }) {
        throw 'Les arguments ne doivent pas contenir de guillemets doubles.'
    }
    # Do not add -quit: the test runner owns shutdown after writing the XML.
    $quotedArguments = ($arguments | ForEach-Object { '"' + $_ + '"' }) -join ' '
    Write-Output "Logs et resultat : $outputPath"
    $process = Start-Process -FilePath $UnityPath -ArgumentList $quotedArguments `
        -WindowStyle Hidden -PassThru
    $process.WaitForExit()
    $unityExitCode = $process.ExitCode
    Set-Content -LiteralPath "$outputPath/exit-code.txt" -Value $unityExitCode
    if ($unityExitCode -ne 0) { throw "Unity a termine avec le code $unityExitCode. Voir $outputPath/editor.log" }
    if (!(Test-Path -LiteralPath $resultsPath)) { throw "Unity n'a pas produit de resultat XML." }
    [xml]$results = Get-Content -LiteralPath $resultsPath -Raw
    $run = $results.'test-run'
    if ([int]$run.total -lt 1 -or $run.result -ne 'Passed') {
        throw "Tests non valides : total=$($run.total), resultat=$($run.result). Voir $resultsPath"
    }
    Write-Output "EditMode : $($run.passed)/$($run.total) reussis."
}
finally {
    foreach ($name in $savedEnvironment.Keys) {
        [Environment]::SetEnvironmentVariable($name, $savedEnvironment[$name], 'Process')
    }
}
