# Przewodnik Migracji ze Starej Wersji

## Główne Zmiany

### Framework
- **Stara wersja**: .NET Framework 4.5.2
- **Nowa wersja**: .NET 8 (działa na Windows 10+)

### Biblioteki Audio
- **Stara wersja**: CSCore z NuGet (wrapper wokół WASAPI)
- **Nowa wersja**:
  - Bezpośrednie P/Invoke dla WASAPI (bez zewnętrznych zależności dla WDM)
  - NAudio.Asio dla obsługi ASIO

### Funkcjonalność

#### Co zostało zachowane
- ✅ Ikona w system tray
- ✅ Menu kontekstowe (Turn ON, Turn OFF, Settings, Exit)
- ✅ Wysyłanie HTTP requestów przy zmianie stanu audio
- ✅ Konfigurowalne opóźnienia
- ✅ 4 URLe (2x ON, 2x OFF)
- ✅ Zapisywanie ustawień

#### Co zostało ulepszone
- ✅ **Wybór urządzeń**: Możesz teraz wybrać, które urządzenia monitorować (wcześniej tylko domyślne)
- ✅ **ASIO**: Pełne wsparcie dla urządzeń ASIO
- ✅ **UI**: Nowoczesny interfejs z zakładkami
- ✅ **Testowanie**: Możliwość testowania URLi bezpośrednio z ustawień
- ✅ **Pause/Resume**: Możliwość wstrzymania monitorowania bez zamykania aplikacji
- ✅ **Format ustawień**: JSON zamiast XML (.NET settings)

#### Co się zmieniło
- ⚠️ **Lokalizacja ustawień**:
  - Stara: `%LOCALAPPDATA%\<CompanyName>\SoundWatcher\<version>\user.config`
  - Nowa: `%APPDATA%\SoundWatcher\settings.json`
- ⚠️ **Kompilacja**: Wymaga .NET 8 SDK lub Build Tools (nie Visual Studio)

## Migracja Ustawień

Stare ustawienia **nie są** automatycznie importowane. Musisz ręcznie przepisać:

### Znajdź stare ustawienia
Lokalizacja: `%LOCALAPPDATA%\<YourUserName>\SoundWatcher.exe_<hash>\<version>\user.config`

### Stary format (XML)
```xml
<setting name="textBoxUrl1" serializeAs="String">
    <value>http://example.com/on1</value>
</setting>
<setting name="textBoxUrl2" serializeAs="String">
    <value>http://example.com/on2</value>
</setting>
<setting name="textBoxUrl3" serializeAs="String">
    <value>http://example.com/off1</value>
</setting>
<setting name="textBoxUrl4" serializeAs="String">
    <value>http://example.com/off2</value>
</setting>
<setting name="delay" serializeAs="String">
    <value>1000</value>
</setting>
<setting name="closeDelay" serializeAs="String">
    <value>30000</value>
</setting>
```

### Nowy format (JSON)
Otwórz nową aplikację, przejdź do Settings i wprowadź te same wartości, lub ręcznie edytuj:

`%APPDATA%\SoundWatcher\settings.json`
```json
{
  "OnUrls": [
    "http://example.com/on1",
    "http://example.com/on2"
  ],
  "OffUrls": [
    "http://example.com/off1",
    "http://example.com/off2"
  ],
  "CheckIntervalMs": 1000,
  "TurnOffDelayMs": 30000,
  "MonitoredDeviceIds": [],
  "MonitoringEnabled": true
}
```

### Mapowanie ustawień

| Stara nazwa | Nowa nazwa | Opis |
|------------|-----------|------|
| `textBoxUrl1` | `OnUrls[0]` | Pierwszy URL ON |
| `textBoxUrl2` | `OnUrls[1]` | Drugi URL ON |
| `textBoxUrl3` | `OffUrls[0]` | Pierwszy URL OFF |
| `textBoxUrl4` | `OffUrls[1]` | Drugi URL OFF |
| `delay` | `CheckIntervalMs` | Interwał sprawdzania |
| `closeDelay` | `TurnOffDelayMs` | Opóźnienie przed OFF |
| *(brak)* | `MonitoredDeviceIds` | **NOWE**: Lista ID urządzeń do monitorowania |

## Pierwsze Uruchomienie Nowej Wersji

1. **Uruchom aplikację** - pojawi się ikona w tray
2. **Kliknij prawym** na ikonę → **Settings**
3. **Zakładka "Audio Devices"**:
   - Kliknij "Refresh Devices"
   - Zaznacz urządzenia do monitorowania (np. domyślne głośniki)
   - Możesz wybrać wiele urządzeń jednocześnie
4. **Zakładka "HTTP Notifications"**:
   - Wprowadź URLe ON/OFF ze starych ustawień
   - Użyj przycisków "Test" aby sprawdzić połączenie
5. **Zakładka "Timing Settings"**:
   - Check interval: To samo co stare `delay` (domyślnie 1000ms)
   - Turn OFF delay: To samo co stare `closeDelay` (domyślnie 30000ms)
6. **Kliknij Save**

## ASIO - Nowa Funkcjonalność

Jeśli masz urządzenie ASIO (np. Focusrite, RME, Steinberg):

1. Upewnij się, że sterowniki ASIO są zainstalowane
2. W zakładce "Audio Devices" zobaczysz urządzenia oznaczone jako `[ASIO]`
3. Zaznacz urządzenie ASIO
4. Kliknij Save

**Uwaga**: Tylko jedno urządzenie ASIO może być monitorowane jednocześnie (ograniczenie ASIO API).

## Troubleshooting Migracji

### "Brak urządzeń w liście"
- Kliknij "Refresh Devices"
- Sprawdź, czy urządzenia audio są włączone w systemie Windows

### "URLe nie działają"
- Użyj przycisków "Test ON URLs" / "Test OFF URLs"
- Sprawdź, czy URLe są poprawne (http:// lub https://)

### "Aplikacja nie startuje"
- Upewnij się, że masz zainstalowany .NET 8 Runtime (Desktop)
- Pobierz: https://dotnet.microsoft.com/download/dotnet/8.0

## Równoległe Użycie Obu Wersji

Możesz używać obu wersji równolegle:
- Stara wersja używa własnych ustawień (.NET Framework config)
- Nowa wersja używa `settings.json`
- **Ale**: Nie uruchamiaj obu wersji jednocześnie (konflikt dostępu do urządzeń audio)

## Zalecenia

1. **Przetestuj nową wersję** z tymi samymi ustawieniami co stara
2. **Sprawdź logi** - nowa wersja wypisuje błędy do konsoli (nie MessageBox)
3. **Użyj zakładki Devices** - sprawdź, czy Twoje urządzenie jest na liście
4. **ASIO**: Jeśli nie potrzebujesz ASIO, po prostu monitoruj urządzenia WASAPI

## Pomocy!

Jeśli coś nie działa:
1. Sprawdź README.md w katalogu projektu
2. Sprawdź sekcję Troubleshooting w README.md
3. Upewnij się, że .NET 8 Runtime jest zainstalowany
