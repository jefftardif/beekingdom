@echo off
setlocal EnableExtensions
title Sauvegarde du travail des agents Bee Kingdom dans la VM

set "SOURCE=C:\projets\beekingdomgame-master"
if not exist "%SOURCE%\ProjectSettings\ProjectVersion.txt" (
    echo Projet local VM introuvable: %SOURCE%
    pause
    exit /b 1
)

for /f %%I in ('powershell.exe -NoProfile -Command "Get-Date -Format yyyyMMdd-HHmmss"') do set "STAMP=%%I"
set "DEST=C:\BeeKingdom_VM_Backups\Agents-BeforeUnityRepair-%STAMP%"

echo Sauvegarde de:
echo   %SOURCE%
echo vers:
echo   %DEST%
echo.

robocopy "%SOURCE%" "%DEST%" /E /COPY:DAT /DCOPY:DAT /R:2 /W:1 /XJ ^
  /XD ".codex" ".git" ".idea" ".utmp" ".vs" ".vscode" "artifacts" ^
      "bin" "Build" "Builds" "DEMO_Evidence_Staging" "Library" "Logs" ^
      "MemoryCaptures" "obj" "outputs" "Temp" "UserSettings" ^
  /XF "*.aab" "*.apk" "*.booproj" "*.csproj" "*.mdb" "*.opendb" ^
      "*.pdb" "*.pidb" "*.sln" "*.suo" "*.svd" "*.tmp" ^
      "*.unitypackage" "*.user" "*.userprefs" /NFL /NDL /NP

set "ROBOCOPY_RESULT=%ERRORLEVEL%"
echo.
if %ROBOCOPY_RESULT% GEQ 8 (
    echo La sauvegarde a echoue. Code Robocopy: %ROBOCOPY_RESULT%
    pause
    exit /b %ROBOCOPY_RESULT%
)

echo Sauvegarde terminee avec succes.
echo Dossier: %DEST%
echo.
echo Ne supprime pas cette sauvegarde avant la synchronisation finale.
pause
exit /b 0
