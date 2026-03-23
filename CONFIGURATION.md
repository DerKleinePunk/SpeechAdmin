# SpeechAdmin - Konfiguration

## appsettings.json

Die Anwendung wird über die `appsettings.json` konfiguriert, die sich im Ausgabeverzeichnis der Anwendung befinden muss.

### Struktur

```json
{
  "Application": {
    "Name": "SpeechAdmin",
    "HotKey": {
      "Modifiers": "Ctrl+Alt",
      "Key": "R"
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "System": "Warning",
      "SpeechAdmin": "Debug"
    },
    "File": {
      "Enabled": true,
      "Path": "logs/speechadmin-.log",
      "RollingInterval": "Day",
      "RetainedFileCountLimit": 7
    },
    "Console": {
      "Enabled": true
    }
  }
}
```

## Konfigurationsoptionen

### Application

#### HotKey
Konfiguration des globalen Hotkeys zum Starten der Aufnahme.

- **Modifiers**: Kombinierbare Modifier-Tasten (getrennt durch "+")
  - Mögliche Werte: `Ctrl`, `Alt`, `Shift`, `Win`
  - Beispiele:
    - `"Ctrl+Alt"` - Strg + Alt
    - `"Ctrl+Shift"` - Strg + Umschalt
    - `"Win+Alt"` - Windows + Alt

- **Key**: Die Taste, die zusammen mit den Modifiers gedrückt werden muss
  - Einzelnes Zeichen (A-Z, 0-9)
  - Beispiele: `"R"`, `"S"`, `"F12"`

### Logging

#### LogLevel
Definiert die Logging-Stufen für verschiedene Namespaces.

- **Default**: Standard-Logging-Level für alle nicht explizit konfigurierten Namespaces
- **Microsoft**: Logging-Level für Microsoft-Framework-Komponenten
- **System**: Logging-Level für System-Komponenten
- **SpeechAdmin**: Logging-Level für die SpeechAdmin-Anwendung

Mögliche Werte (von wenig bis viel):
- `None` - Kein Logging
- `Critical` - Nur kritische Fehler
- `Error` - Fehler
- `Warning` - Warnungen und Fehler
- `Information` - Informative Meldungen (empfohlen)
- `Debug` - Debug-Informationen
- `Trace` - Detaillierte Trace-Informationen

#### File
Konfiguration des File-Logging (Logs werden in Dateien geschrieben).

- **Enabled**: `true` oder `false` - Aktiviert/Deaktiviert File-Logging
- **Path**: Pfad zur Log-Datei
  - Kann einen Platzhalter für das Datum enthalten (wird durch `RollingInterval` gesteuert)
  - Beispiele:
    - `"logs/speechadmin-.log"` - Erstellt Dateien wie `speechadmin-20240101.log`
    - `"logs/app.log"` - Einfache Log-Datei ohne Rotation

- **RollingInterval**: Interval für das Erstellen neuer Log-Dateien
  - Mögliche Werte: `Infinite`, `Year`, `Month`, `Day`, `Hour`, `Minute`
  - Empfohlen: `Day` (täglich neue Log-Datei)

- **RetainedFileCountLimit**: Anzahl der zu behaltenden Log-Dateien
  - `null` - Alle Log-Dateien behalten
  - Zahl (z.B. `7`) - Nur die letzten X Dateien behalten

#### Console
Konfiguration des Console-Logging (Logs werden in die Konsole geschrieben).

- **Enabled**: `true` oder `false` - Aktiviert/Deaktiviert Console-Logging

## Beispiel-Konfigurationen

### Minimales Logging (nur Fehler in Datei)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Error"
    },
    "File": {
      "Enabled": true,
      "Path": "logs/errors-.log",
      "RollingInterval": "Day",
      "RetainedFileCountLimit": 30
    },
    "Console": {
      "Enabled": false
    }
  }
}
```

### Debug-Modus (alles loggen)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Debug",
      "System": "Debug",
      "SpeechAdmin": "Trace"
    },
    "File": {
      "Enabled": true,
      "Path": "logs/debug-.log",
      "RollingInterval": "Hour",
      "RetainedFileCountLimit": 24
    },
    "Console": {
      "Enabled": true
    }
  }
}
```

### Alternativer Hotkey (Windows+S)

```json
{
  "Application": {
    "HotKey": {
      "Modifiers": "Win",
      "Key": "S"
    }
  }
}
```

## Logs-Verzeichnis

Die Log-Dateien werden standardmäßig im Unterordner `logs/` gespeichert. Dieser Ordner wird automatisch erstellt, wenn er nicht existiert.

Der `logs/` Ordner wird vom Git-Repository ignoriert (siehe `.gitignore`).
