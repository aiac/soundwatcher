@echo off
REM Build script for SoundWatcher using MSBuild
REM Requires: .NET 8 SDK or Build Tools for Visual Studio with .NET 8 support

echo ========================================
echo Building SoundWatcher
echo ========================================
echo.

REM Try to find MSBuild
set MSBUILD_PATH=

REM Check for .NET SDK first (preferred)
dotnet --version >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo Found .NET SDK
    dotnet build -c Release
    if %ERRORLEVEL% EQU 0 (
        echo.
        echo ========================================
        echo Build completed successfully!
        echo Output: bin\Release\net8.0-windows\
        echo ========================================
        exit /b 0
    ) else (
        echo .NET SDK found but build failed, trying MSBuild...
    )
)

REM If dotnet not found, try MSBuild
echo .NET SDK not found, looking for MSBuild...

REM Try Visual Studio 2022
if exist "C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe
)
if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe
)
if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe
)
if exist "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe
)

REM Try Visual Studio 2019
if "%MSBUILD_PATH%"=="" (
    if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe" (
        set MSBUILD_PATH=C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe
    )
    if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" (
        set MSBUILD_PATH=C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe
    )
)

if "%MSBUILD_PATH%"=="" (
    echo ERROR: Neither .NET SDK nor MSBuild found!
    echo.
    echo Please install one of the following:
    echo 1. .NET 8 SDK from https://dotnet.microsoft.com/download
    echo 2. Build Tools for Visual Studio from https://visualstudio.microsoft.com/downloads/
    echo.
    exit /b 1
)

echo Found MSBuild: %MSBUILD_PATH%
echo.

"%MSBUILD_PATH%" SoundWatcher.csproj /p:Configuration=Release /restore

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo Build completed successfully!
    echo Output: bin\Release\net8.0-windows\
    echo ========================================
) else (
    echo Build failed!
    exit /b 1
)
