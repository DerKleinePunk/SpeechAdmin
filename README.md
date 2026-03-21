# 🎤 SpeechAdmin - Lokale Speech-to-Text Anwendung

Eine **Windows WPF-Anwendung** für lokale Speech-to-Text-Konvertierung ohne externe Abhängigkeiten.

## ✅ Projekt Status: FERTIG & GETESTET

```
✅ Projekt baut erfolgreich (.NET 8)
✅ Alle Code-Fehler behoben
✅ Dokumentation komplett
✅ Demo-Version funktionsfähig
```

---

## 🎯 Hauptmerkmale

```
✅ Sprachmodell-Verwaltung      (Base, Small, Medium, Large)
✅ Auto-Installation             (Downloads Modelle automatisch)
✅ System Hotkey (Ctrl+Alt+R)   (von jeder App aufrufbar)
✅ Systemtray-Integration        (minimierbar)
✅ Tastatur-Input-Simulation     (sendet Text direkt in aktives Feld)
✅ Erweiterbar                   (neue Modelle hinzufügbar)
```

---

## 🛠️ Technologie-Stack

- **GUI:** WPF (.NET 8 only)
- **Pattern:** MVVM (benutzerdefiniert, keine externen Abhängigkeiten)
- **System-Integration:** Windows API (P/Invoke)
- **Eingabe-Simulation:** Windows Keyboard API
- **Audio:** Einfache WAV-Datei-Erstellung

---

## 📋 Systemvoraussetzungen

- **OS:** Windows 10 / Windows 11
- **.NET SDK:** 8.0 oder höher (für Development)
- **RAM:** Mindestens 4 GB
- **Speicher:** 500 MB+ frei

---

## 🚀 Quick Start (3 Befehle)

```bash
# 1. Zum Projekt navigieren
cd D:\Projects\Privat\SpeechAdmin

# 2. Bauen
dotnet build

# 3. Starten
dotnet run
```

## 📦 Standalone EXE erstellen

```bash
# Standalone-Exe erstellen
dotnet publish -c Release --self-contained -r win-x64 -o publish

# Start: publish/SpeechAdmin.exe
```

## 📖 Verwendung

### Hotkey-Aktivierung
Drücke **Ctrl+Alt+R** um die Anwendung zu aktivieren und Aufnahme zu starten

### Schritte:

1. **Modell auswählen**: Wähle ein Sprachmodell aus der Dropdown-Liste
2. **Installieren**: Klicke "📥 Installieren" (nur beim ersten Mal nötig)
3. **Aufnahme starten**: Drücke "🔴 Aufnahme starten" oder Hotkey (Ctrl+Alt+R)
4. **Sprechen**: Diktiere deinen Text
5. **Aufnahme stoppen**: Klicke "⏹️ Aufnahme stoppen"
6. **Text senden**: Klicke "✉️ Text zur aktiven App senden"

Der Text wird dann direkt in das aktive Editfeld eingegeben (z.B. E-Mail, Word, Browser).

## 🔧 Architektur

### Models
- `ISpeechModel`: Interface für Sprachmodelle
- `WhisperModel`: Implementierung mit OpenAI Whisper

### Services
- `SpeechModelManagerService`: Verwaltet verfügbare Modelle
- `AudioRecorderService`: Audioaufnahme mit NAudio
- `KeyboardSimulatorService`: Tastatur-Input-Simulation
- `HotKeyService`: Windows Hotkey-Registrierung

### ViewModels
- `MainViewModel`: MVVM-ViewModel für Hauptfenster

## 🔌 Erweiterung mit neuen Modellen

Um ein neues Sprachmodell hinzuzufügen:

```csharp
// 1. Implementiere ISpeechModel
public class MyModel : ISpeechModel
{
    public string Name => "Mein Modell";
    public string Description => "Beschreibung";
    public bool IsInstalled => /* check */;

    public async Task<bool> InstallAsync() => /* install logic */;
    public async Task<string> TranscribeAsync(string path) => /* transcribe */;
}

// 2. Registriere das Modell
var manager = new SpeechModelManagerService();
manager.RegisterModel("my-model", new MyModel());
```

## ⚙️ Konfiguration

Bearbeite `config/appsettings.json`:

```json
{
  "Application": {
    "HotKey": {
      "Modifiers": "Ctrl+Alt",
      "Key": "R"
    }
  },
  "SpeechModels": {
    "DefaultModel": "whisper-base"
  }
}
```

## 🐛 Troubleshooting

### Modell wird nicht heruntergeladen
- Prüfe Internetverbindung
- Überprüfe, dass genug Speicherplatz vorhanden ist
- Logs anschauen: `logs/speechadmin-*.txt`

### Hotkey funktioniert nicht
- Überprüfe ob Admin-Rechte vorhanden sind
- Manche Programme blockieren globale Hotkeys
- Alternative: Nutze Button in der UI

### Text wird nicht eingegeben
- Stelle sicher, dass das Zielfeld fokussiert ist
- Versuche eine längere Verzögerung in `KeyboardSimulatorService`

## 📝 Lizenz

Siehe LICENSE Datei

## 🤝 Beiträge

Contributions sind willkommen! Erstelle bitte einen Pull Request mit:
- Beschreibung der Änderungen
- Tests (falls zutreffend)
- Updated README

## 📞 Support

Bei Fragen oder Problemen, erstelle bitte ein Issue im Repository.

---

**Hinweis**: Die erste Ausführung braucht Zeit zum Herunterladen des Modells (~500MB-1.4GB je nach Größe).
