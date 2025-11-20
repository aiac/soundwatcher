@echo off
echo ============================================
echo Building SoundWatcher as single-file EXE
echo ============================================
echo.

echo Publishing Release build...
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ============================================
    echo SUCCESS!
    echo ============================================
    echo.
    echo Single-file EXE created at:
    echo bin\Release\net8.0-windows\win-x64\publish\SoundWatcher.exe
    echo.
    echo This version requires .NET 8 Runtime to be installed.
    echo.
) else (
    echo.
    echo ============================================
    echo BUILD FAILED
    echo ============================================
    echo.
)

pause
