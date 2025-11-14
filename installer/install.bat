@echo off
setlocal enabledelayedexpansion

echo ==========================================
echo 🎮 MIMESIS MorePlayers Installer v1.8.0
echo ==========================================
echo.

:: Find Steam installation
set "GAME_PATH="

echo 🔍 Searching for MIMESIS...

:: Check common Steam paths
for %%P in (
    "C:\Program Files (x86)\Steam\steamapps\common\MIMESIS"
    "C:\Program Files\Steam\steamapps\common\MIMESIS"
    "D:\SteamLibrary\steamapps\common\MIMESIS"
    "E:\SteamLibrary\steamapps\common\MIMESIS"
) do (
    if exist %%P (
        set "GAME_PATH=%%~P"
        echo ✓ Found MIMESIS at: !GAME_PATH!
        goto :found
    )
)

:notfound
echo ⚠ Could not auto-detect MIMESIS
set /p GAME_PATH="Enter MIMESIS path manually: "
if not exist "%GAME_PATH%" (
    echo ✗ Invalid path
    pause
    exit /b 1
)

:found
echo.

:: Check MelonLoader
if not exist "%GAME_PATH%\MelonLoader" (
    echo ⚠ MelonLoader not found
    echo 📦 Please install MelonLoader first:
    echo    1. Download from: https://github.com/LavaGang/MelonLoader/releases
    echo    2. Run MelonLoader.Installer.exe
    echo    3. Select MIMESIS
    echo    4. Run this installer again
    echo.
    pause
    exit /b 1
)

echo ✓ MelonLoader found
echo.

:: Install mod
set "MODS_PATH=%GAME_PATH%\Mods"
if not exist "%MODS_PATH%" mkdir "%MODS_PATH%"

copy /Y "%~dp0bin\MorePlayers.dll" "%MODS_PATH%\MorePlayers.dll" >nul
echo ✓ Installed MorePlayers mod

echo.
echo ==========================================
echo ✅ Installation Complete!
echo ==========================================
echo.
echo 🎮 Launch MIMESIS and create a lobby
echo 👥 You can now host 5+ player games!
echo.
echo Note: Only the HOST needs this mod
echo.
pause
