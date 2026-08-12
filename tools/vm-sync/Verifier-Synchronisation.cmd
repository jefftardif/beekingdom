@echo off
setlocal
title Verification synchronisation Bee Kingdom

echo Verification des differences sans modifier aucun fichier...
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0BeeKingdom-VmSync.ps1" -Mode Status
set "RESULT=%ERRORLEVEL%"

echo.
echo Rapport:
echo C:\projets\beekingdomgame-master\.codex\vm-sync-last-report.txt
echo.
pause
exit /b %RESULT%
