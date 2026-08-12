@echo off
setlocal
title Synchronisation Bee Kingdom

echo Synchronisation Bee Kingdom entre la VM et l'ordinateur principal...
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0BeeKingdom-VmSync.ps1" -Mode Sync
set "RESULT=%ERRORLEVEL%"

echo.
if "%RESULT%"=="0" (
    echo Synchronisation terminee sans conflit.
) else if "%RESULT%"=="2" (
    echo Des conflits ou suppressions en attente sont inscrits dans:
    echo C:\projets\beekingdomgame-master\.codex\vm-sync-last-report.txt
) else (
    echo La synchronisation a echoue. Code: %RESULT%
)
echo.
pause
exit /b %RESULT%
