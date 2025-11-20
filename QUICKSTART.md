# Szybki Start - SoundWatcher

## 🚀 W 3 krokach do działającej aplikacji

### Krok 1: Zainstaluj .NET 8 SDK

**Nie masz jeszcze .NET 8 SDK?**

Uruchom w tym katalogu:
```bash
download-sdk.bat
```

To otworzy stronę pobierania. Pobierz i zainstaluj .NET 8 SDK dla Windows.

**Lub pobierz bezpośrednio:**
- Windows x64: https://dotnet.microsoft.com/download/dotnet/8.0

Po instalacji **zamknij i otwórz ponownie terminal**.

### Krok 2: Kompiluj

```bash
build.bat
```

Poczekaj, aż kompilacja się zakończy. Powinieneś zobaczyć:
```
Build completed successfully!
Output: bin\Release\net8.0-windows\
```

### Krok 3: Uruchom

```bash
bin\Release\net8.0-windows\SoundWatcher.exe
```

Lub po prostu przejdź do folderu `bin\Release\net8.0-windows\` i uruchom `SoundWatcher.exe`.

## ⚙️ Pierwsza konfiguracja

1. **Znajdź ikonę w system tray** (prawy dolny róg ekranu, przy zegarze)
2. **Kliknij prawym przyciskiem** na ikonę → **Settings**
3. **Zakładka "Audio Devices"**:
   - Zaznacz urządzenia, które chcesz monitorować (np. Speakers)
4. **Zakładka "HTTP Notifications"**:
   - Wpisz URLe, które mają być wywoływane przy włączeniu/wyłączeniu dźwięku
   - Użyj przycisków "Test" aby sprawdzić
5. **Kliknij Save**

## ✅ Gotowe!

Aplikacja teraz monitoruje wybrane urządzenia audio i wysyła powiadomienia HTTP.

---

## 📚 Więcej informacji

- **Pełna dokumentacja**: [README.md](README.md)
- **Instrukcja instalacji SDK**: [INSTALL_SDK.md](INSTALL_SDK.md)
- **Migracja ze starej wersji**: [MIGRATION_GUIDE.md](MIGRATION_GUIDE.md)

## ❓ Problemy?

### Kompilacja nie działa
- Sprawdź, czy .NET 8 SDK jest zainstalowane: `dotnet --version`
- Powinno pokazać wersję 8.0.x

### Aplikacja nie wykrywa urządzeń
- Kliknij "Refresh Devices" w ustawieniach
- Upewnij się, że urządzenia audio są włączone w systemie

### URLe nie działają
- Sprawdź URLe w zakładce "HTTP Notifications"
- Użyj przycisków "Test ON URLs" / "Test OFF URLs"
