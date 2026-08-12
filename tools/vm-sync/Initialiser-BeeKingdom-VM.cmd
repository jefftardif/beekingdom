@echo off
setlocal
title Initialisation Bee Kingdom dans la VM

echo Initialisation de la copie locale Bee Kingdom...
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0BeeKingdom-VmSync.ps1" -Mode Initialize
set "RESULT=%ERRORLEVEL%"

echo.
if "%RESULT%"=="0" (
    echo Initialisation terminee avec succes.
) else (
    echo L'initialisation n'a pas abouti. Code: %RESULT%
)
echo.
pause
exit /b %RESULT%
