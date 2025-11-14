@echo off
echo Building MorePlayers Installer...
echo.

:: Build the installer
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✓ Build successful!
    echo.
    echo Output: bin\Release\net6.0-windows\win-x64\publish\MorePlayers-Installer.exe
    echo.
) else (
    echo.
    echo ✗ Build failed
    echo.
)

pause
