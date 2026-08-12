@echo off
setlocal

fltmc >nul 2>&1
if %errorlevel% equ 0 goto elevated

echo Bee Kingdom demande les droits administrateur pour creer le partage prive.
powershell.exe -NoProfile -WindowStyle Hidden -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
exit /b

:elevated
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Configurer-Partage-Hote.ps1"
set "RESULT=%errorlevel%"
echo.
if not "%RESULT%"=="0" echo La configuration n'a pas abouti. Code: %RESULT%
pause
exit /b %RESULT%
