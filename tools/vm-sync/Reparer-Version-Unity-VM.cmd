@echo off
setlocal
title Reparation version Unity Bee Kingdom dans la VM

echo Reparation limitee de ProjectSettings\ProjectVersion.txt...
echo Unity doit etre ferme dans la VM.
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Reparer-Version-Unity-VM.ps1" -HostRoot "%~dp0..\.."
set "RESULT=%ERRORLEVEL%"

echo.
if "%RESULT%"=="0" (
    echo Version du projet restauree. Utilise maintenant Unity 6000.5.3f1.
) else (
    echo La reparation n'a pas abouti. Code: %RESULT%
)
echo.
pause
exit /b %RESULT%
