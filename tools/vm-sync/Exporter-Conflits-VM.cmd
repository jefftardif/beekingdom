@echo off
setlocal
title Export des conflits Bee Kingdom

echo Export des versions VM et ordinateur des fichiers en conflit...
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Exporter-Conflits-VM.ps1"
set "RESULT=%ERRORLEVEL%"

echo.
if "%RESULT%"=="0" (
    echo Export termine sans modifier le projet.
) else (
    echo L'export a echoue. Code: %RESULT%
)
echo.
pause
exit /b %RESULT%

