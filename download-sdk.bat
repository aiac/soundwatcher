@echo off
REM Helper script to download .NET 8 SDK installer

echo ========================================
echo .NET 8 SDK Download Helper
echo ========================================
echo.

REM Check if .NET 8 SDK is already installed
dotnet --version >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo .NET SDK is already installed!
    dotnet --version
    echo.
    echo You can now run: build.bat
    pause
    exit /b 0
)

echo .NET SDK not found. Opening download page...
echo.
echo Please download and install .NET 8 SDK for Windows x64
echo.
echo After installation:
echo 1. Close and reopen this terminal
echo 2. Run: build.bat
echo.

REM Open the download page in default browser
start https://dotnet.microsoft.com/download/dotnet/8.0

echo.
echo Download page opened in your browser.
echo.
pause
