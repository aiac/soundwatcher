@echo off
echo ============================================
echo Building SoundWatcher as standalone EXE
echo (includes .NET runtime - no install needed)
echo ============================================
echo.

echo Publishing self-contained Release build...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ============================================
    echo SUCCESS!
    echo ============================================
    echo.
    echo Standalone single-file EXE created at:
    echo bin\Release\net8.0-windows\win-x64\publish\SoundWatcher.exe
    echo.
    echo This version includes .NET runtime and works without any installation.
    echo File size will be larger (~70-80 MB) but fully portable.
    echo.
) else (
    echo.
    echo ============================================
    echo BUILD FAILED
    echo ============================================
    echo.
)

pause
