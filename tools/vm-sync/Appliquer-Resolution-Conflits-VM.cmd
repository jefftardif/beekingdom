@echo off
setlocal
title Resolution des conflits Bee Kingdom

echo Sauvegarde des versions VM et application des documents fusionnes...
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Appliquer-Resolution-Conflits-VM.ps1"
set "RESULT=%ERRORLEVEL%"

echo.
if "%RESULT%"=="0" (
    echo Resolution appliquee sans perte.
) else (
    echo La resolution a echoue. Code: %RESULT%
)
echo.
pause
exit /b %RESULT%

