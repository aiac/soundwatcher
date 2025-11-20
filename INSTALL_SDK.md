# Instalacja .NET 8 SDK

Projekt wymaga .NET 8 SDK do kompilacji. Oto najszybsze sposoby instalacji:

## Opcja 1: Oficjalny instalator (Zalecane)

### Bezpośredni link do pobrania:
**Windows x64**: https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-8.0.404-windows-x64-installer

Lub odwiedź: https://dotnet.microsoft.com/download/dotnet/8.0

### Kroki:
1. Pobierz instalator (ok. 200 MB)
2. Uruchom `dotnet-sdk-8.0.xxx-win-x64.exe`
3. Zaakceptuj licencję i kliknij Install
4. Po instalacji zamknij i otwórz ponownie terminal
5. Sprawdź instalację: `dotnet --version`

## Opcja 2: Winget (Windows Package Manager)

Jeśli masz Windows 10/11 z winget:

```bash
winget install Microsoft.DotNet.SDK.8
```

## Opcja 3: Chocolatey

Jeśli używasz Chocolatey:

```bash
choco install dotnet-sdk -y
```

## Opcja 4: Portable (bez instalacji)

Możesz użyć portable wersji bez instalatora:

1. Pobierz: https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-8.0.404-windows-x64-binaries
2. Rozpakuj do np. `C:\dotnet-sdk`
3. Dodaj do PATH: `C:\dotnet-sdk`
4. Zrestartuj terminal

## Weryfikacja instalacji

Po instalacji uruchom w terminalu:

```bash
dotnet --version
```

Powinieneś zobaczyć wersję 8.0.x

## Kompilacja projektu

Po zainstalowaniu .NET 8 SDK:

```bash
cd d:\dev\soundwatcher\SoundWatcher
build.bat
```

lub bezpośrednio:

```bash
dotnet build -c Release
```

## Alternatywa: Visual Studio 2022 Build Tools

Jeśli nie chcesz instalować .NET SDK, możesz zainstalować:

**Build Tools for Visual Studio 2022**: https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022

Podczas instalacji wybierz:
- ".NET desktop build tools"
- ".NET 8.0 Runtime"

## Minimalne wymagania systemowe

- Windows 10 version 1607 lub nowszy
- Windows 11
- Windows Server 2016 lub nowszy

## Troubleshooting

### "dotnet command not found" po instalacji
- Zrestartuj terminal/PowerShell
- Sprawdź PATH: `echo %PATH%` (cmd) lub `$env:Path` (PowerShell)
- Poszukaj: `C:\Program Files\dotnet\dotnet.exe`

### "The current .NET SDK does not support targeting .NET 8.0"
- Masz starszą wersję SDK
- Uruchom: `dotnet --list-sdks`
- Zainstaluj .NET 8 SDK używając linków powyżej

### Mam Visual Studio 2019, czy to wystarczy?
- Nie, VS 2019 nie wspiera .NET 8
- Musisz zainstalować .NET 8 SDK osobno
- Lub zaktualizować do Visual Studio 2022
