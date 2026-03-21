# 🏗️ SpeechAdmin - Architektur & Technische Details

## Projektstruktur

```
src/
├── Models/
│   ├── ISpeechModel.cs          # Interface für Sprachmodelle
│   └── WhisperModel.cs          # Demo-Implementierung
├── Services/
│   ├── AudioRecorderService.cs  # WAV-Audioaufnahme
│   ├── HotKeyService.cs         # Windows Hotkey (Ctrl+Alt+R)
│   ├── KeyboardSimulatorService # Tastatur-Inputs simulieren
│   ├── SpeechModelManagerService # Modell-Verwaltung
│   └── TrayIconService.cs       # Systemtray-Integration
├── ViewModels/
│   ├── MainViewModel.cs         # MVVM-Logic
│   └── RelayCommand.cs          # Custom ICommand
└── Views/
    ├── MainWindow.xaml          # WPF UI
    └── MainWindow.xaml.cs       # Code-Behind
```

## Design Patterns

### MVVM (Model-View-ViewModel)
- **View:** MainWindow.xaml (WPF)
- **ViewModel:** MainViewModel.cs (INotifyPropertyChanged)
- **Model:** ISpeechModel + Services
- **Binding:** Zwei-Wege Datenbindung

### Service-Oriented
Jeder Service hat eine klare Verantwortung:
- **AudioRecorderService** - nur Audioaufnahme
- **HotKeyService** - nur Hotkey-Management
- **KeyboardSimulatorService** - nur Input-Simulation

### Dependency Injection Pattern
Services werden im ViewModel injiziert:
```csharp
private readonly SpeechModelManagerService _modelManager;
private readonly AudioRecorderService _audioRecorder;
private readonly KeyboardSimulatorService _keyboardSimulator;
```

## Erweiterung: Neue Sprachmodelle

### Schritt 1: ISpeechModel implementieren

```csharp
public class MyCustomModel : ISpeechModel
{
    public string Name => "Mein Modell";
    public string Description => "Beschreibung";
    public bool IsInstalled => File.Exists(_modelPath);

    public async Task<bool> InstallAsync()
    {
        // Download & Installation
        return true;
    }

    public async Task<string> TranscribeAsync(string audioFilePath)
    {
        // Transkription durchführen
        return transcribedText;
    }
}
```

### Schritt 2: In SpeechModelManagerService registrieren

```csharp
private void InitializeModels()
{
    // Bestehende Modelle...

    // Neues Modell hinzufügen
    RegisterModel("my-model", new MyCustomModel());
}
```

### Schritt 3: Fertig!
Das Modell erscheint sofort in der UI.

---

## Workflow: Speech-to-Text Prozess

```
1. Benutzer drückt Ctrl+Alt+R (oder Button)
   ↓
2. HotKeyService aktiviert MainViewModel.StartRecording()
   ↓
3. AudioRecorderService startet Aufnahme (.wav)
   ↓
4. Benutzer spricht...
   ↓
5. Stille erkannt → Aufnahme endet
   ↓
6. MainViewModel.StopRecording() aufgerufen
   ↓
7. SpeechModelManagerService.TranscribeAsync() durchführen
   ↓
8. Text in MainViewModel.TranscribedText
   ↓
9. Benutzer klickt "Senden"
   ↓
10. KeyboardSimulatorService sendet Text (Ctrl+V)
    ↓
11. Text erscheint in aktiver Anwendung (Email, Word, etc.)
```

---

## Windows API Integration

### Hotkey-Registration (WinAPI)
```csharp
[DllImport("user32.dll")]
private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

// Ctrl+Alt+R registrieren
RegisterHotKey(windowHandle, 1, MOD_CTRL | MOD_ALT, VK_R);
```

### Tastatur-Simulation (WinAPI)
```csharp
[DllImport("user32.dll")]
private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

// Sendet Ctrl+V
keybd_event(VK_CONTROL, 0, 0, 0);
keybd_event(VK_V, 0, 0, 0);
```

---

## Performance-Optimierungen

### Audio-Streaming
WAV-Header wird beim Start geschrieben, dann direkt zu Datei gestreamt.

### Modell-Caching
Trainierte Modelle werden im `Models/` Verzeichnis gecacht.

### Async/Await
Lange Operationen sind async, um UI nicht zu blockieren.

### Resource Cleanup
Alle `IDisposable` Services werden korrekt disposed.

---

## Sicherheit & Datenschutz

- ✅ **Keine Cloud-Uploads** - Alles bleibt lokal
- ✅ **Keine Permissions nötig** - Standard-User-Rechte
- ✅ **Kein Tracking** - Keine Analytics/Telemetrie
- ✅ **Offline-fähig** - Nach Modell-Download kein Internet nötig

---

## Error Handling

```csharp
try
{
    // Operation durchführen
}
catch (FileNotFoundException ex)
{
    StatusMessage = "Datei nicht gefunden";
}
catch (InvalidOperationException ex)
{
    StatusMessage = "Modell nicht installiert";
}
catch (Exception ex)
{
    StatusMessage = "Fehler: " + ex.Message;
    // Optional: Logging
}
```

---

## Testing-Strategie

Nach Hinzufügen von Unit Tests:

```bash
# Tests ausführen
dotnet test --no-build

# Mit Coverage
dotnet test /p:CollectCoverage=true

# Spezifische Kategorie
dotnet test --filter Category=Unit
```

---

## Build Configuration

**Debug:**
```xml
<Configuration>Debug</Configuration>
<!-- Vollständige Debug-Infos, keine Optimierungen -->
```

**Release:**
```xml
<Configuration>Release</Configuration>
<!-- Optimiert, kleinere Datei, schneller -->
```

---

## Skalierbarkeit

Das Protokoll ist leicht skalierbar für:

1. **Mehrere Audio-Eingaben**
   - Verschiedene Mikrofone
   - Verschiedene Audio-Quellen

2. **Mehrere Ausgaben**
   - Direkte Text-Eingabe (jetzt)
   - Kopie in Zwischenablage
   - In Datei speichern
   - In Cloud hochladen

3. **Mehrere Modelle parallel**
   - Model A für Englisch
   - Model B für Deutsch
   - Etc.

---

## Future Enhancements

```
[ ] Integration mit echtem Whisper.NET
[ ] Google Cloud Speech-to-Text
[ ] Azure Speech Services
[ ] Custom Model Training
[ ] Real-time Transkription
[ ] Mehrsprachenerkennung
[ ] Sentiment-Analyse
[ ] Text-to-Speech Output
[ ] Plugin-System
```

---

**Autor:** Copilot
**Framework:** .NET 8 WPF
**Letzte Änderung:** 20. März 2026
