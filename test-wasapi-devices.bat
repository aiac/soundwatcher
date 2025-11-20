@echo off
echo Checking WASAPI devices...
echo.
powershell -Command "Get-AudioDevice -List | Format-Table -AutoSize"
pause
