# 🎤 SpeechAdmin - Schnellstart Guide

## Was ist SpeechAdmin?

Eine **Windows WPF-Anwendung**, die lokal (ohne Internet) Sprache in Text umwandelt mithilfe von OpenAI Whisper.

## ✨ Hauptmerkmale

```
✅ Lokale Sprachverarbeitung      (kein Cloud-Upload)
✅ Modell-Verwaltung             (Base, Small, Medium, Large)
✅ Auto-Installation             (ladet Modelle automatisch)
✅ System Hotkey (Ctrl+Alt+R)    (von überall aufrufbar)
✅ Tastbar-Integration           (minimierbar in Systemtray)
✅ Direkte Text-Eingabe          (simuliert Tastatureingaben)
✅ Erweiterbar                   (neue Modelle hinzufügbar)
```

## 🚀 Erste Schritte

### 1. **Repository klonen**
```bash
git clone <repo-url>
cd SpeechAdmin
```

### 2. **Projekt bauen**
```bash
dotnet restore
dotnet build
```

### 3. **Starten**
```bash
dotnet run
```

## 📖 Bedienung

### Methode 1: Über Hotkey (empfohlen)
```
1. Drücke: Ctrl + Alt + R
2. Sprich in dein Mikrofon
3. Aufnahme endet automatisch nach Stille (~10 Sekunden)
4. Text wird in dein aktives Editfeld eingefügt
```

### Methode 2: Über die UI
```
1. Wähle Sprachmodell
2. Klicke "Installieren" (wenn noch nicht geschehen)
3. Klicke "🔴 Aufnahme starten"
4. Sprich dein Text
5. Klicke "⏹️ Aufnahme stoppen"
6. Klicke "✉️ Text zur aktiven App senden"
```

## 🛠️ Installation des Modells

**Beim ersten Start:**
- Größe "base" (~140MB) ≈ 2-3 Minuten
- Größe "small" (~465MB) ≈ 5-10 Minuten
- Größe "medium" (~1.4GB) ≈ 15-20 Minuten

Das Modell wird in den `Models/` Ordner heruntergeladen.

## 💡 Tipps & Tricks

| Problem | Lösung |
|---------|--------|
| Text wird nicht eingefügt | Stelle sicher, dass dein Zielfeld (Email, Word, etc.) fokussiert ist |
| Hotkey funktioniert nicht | Starte die App als Administrator |
| Transkription ist schlecht | Versuche ein größeres Modell (Medium/Large) |
| App reagiert langsam | Das Modell wird beim ersten Gebrauch initialisiert |

## 📋 Systemvoraussetzungen

- **OS**: Windows 10 / 11
- **RAM**: Mindestens 4GB (8GB empfohlen für größere Modelle)
- **Speicher**: 2GB+ für Modelle
- **Mikrofon**: Funktionierendes Eingabe-Gerät
- **.NET**: Version 8.0+

## 🔧 Projekt-Struktur

```
SpeechAdmin/
├── src/
│   ├── Models/               # Sprachmodell-Abstraktionen
│   │   ├── ISpeechModel.cs
│   │   └── WhisperModel.cs
│   ├── Services/             # Geschäftslogik-Services
│   │   ├── AudioRecorderService.cs
│   │   ├── HotKeyService.cs
│   │   ├── KeyboardSimulatorService.cs
│   │   ├── SpeechModelManagerService.cs
│   │   └── TrayIconService.cs
│   ├── ViewModels/           # MVVM ViewModels
│   │   └── MainViewModel.cs
│   └── Views/                # WPF Interfaces
│       ├── MainWindow.xaml
│       └── MainWindow.xaml.cs
├── config/
│   └── appsettings.json
├── Models/                   # (wird erstellt) Heruntergeladene Modelle
├── logs/                     # (wird erstellt) Anwendungs-Logs
├── README.md                 # Ausführliche Dokumentation
└── SpeechAdmin.csproj        # Projekt-Konfiguration
```

## 🔌 Erweiterung - Neues Sprachmodell hinzufügen

```csharp
// 1. Neue Klasse erstellen (z.B. GoogleSpeechModel.cs)
public class GoogleSpeechModel : ISpeechModel
{
    public string Name => "Google Speech";
    public string Description => "Google's Speech-to-Text API";
    public bool IsInstalled => /* check */;

    public async Task<bool> InstallAsync() { /* ... */ }
    public async Task<string> TranscribeAsync(string audio) { /* ... */ }
}

// 2. In SpeechModelManagerService.InitializeModels() registrieren
RegisterModel("google-speech", new GoogleSpeechModel());

// 3. Fertig! Modell erscheint automatisch in der UI
```

## 🐛 Debugging

**Logs anschauen:**
```bash
# Logs werden gespeichert in:
logs/speechadmin-*.txt
```

**Modelle überprüfen:**
```bash
# Heruntergeladene Modell-Dateien
dir Models\
```

## 📝 Lizenz

MIT License - siehe LICENSE Datei

## 🤝 Mitwirkende

Contributions sind willkommen! Bitte erstelle einen Pull Request.

---

**Hinweis**: Die Transkription erfolgt lokal auf deinem Computer - keine Daten werden an externe Server gesendet!
