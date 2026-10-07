# Blumind auf .NET 10

Windows-Mindmapping mit der bestehenden WinForms-Oberfläche, modernisierter Laufzeit und kompatiblem `.bmd`-Format. Die erste Testversion dient der Prüfung der vertrauten Abläufe, bevor größere UI- und Funktionsänderungen folgen.

## Voraussetzungen

- Für die Entwicklung: Windows und .NET 10 SDK. `global.json` erlaubt aktuelle .NET-10-SDK-Featureversionen.
- Für die portable x64-Ausgabe: ein von .NET 10 unterstütztes Windows-x64-System. Die .NET-Laufzeit ist enthalten.
- Für formatierte Notizen: Microsoft Edge WebView2 Runtime. Fehlt sie, steht ein HTML-Quelltexteditor als Rückfall bereit.
- Kein altes Windows SDK, Visual Studio 2010/2012, ILMerge oder lokales PDFsharp-DLL erforderlich.

## Bauen, testen und paketieren

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Build.ps1
```

Das Skript stellt festgelegte NuGet-Pakete wieder her, baut die Solution, startet die Integrationstests und erzeugt `artifacts/Blumind-net10-win-x64/` sowie eine ZIP-Datei. Tests öffnen unsichtbare Fenster und benötigen eine Windows-Desktopsitzung mit WebView2. Ausgaben und Testprofil liegen in `artifacts/tests/`. Die originalen Beispielkarten werden nicht überschrieben.

Ohne interaktive Tests: `scripts/Build.ps1 -SkipTests`. Das ersetzt keine erfolgreich ausgeführten Regressionstests.

Die ursprünglichen Ressourcen-/Installerprojekte bleiben als historische Referenz im Repository; sie gehören nicht mehr zum aktuellen Build. Die alten .NET-2-Kompatibilitätsklassen werden nicht mehr kompiliert.

## Testversion verwenden

ZIP vollständig entpacken und `Start-Portable.cmd` starten. Für weitere Hinweise und den Prüfablauf siehe [Testversion](Documents/Modernisierung-Testversion.md).

Der bisherige WinForms-Designer-Vertrag bleibt erhalten. Die neue Designer-Regel WFO1000 ist während dieser Migration gezielt ausgenommen; die Laufzeit- und übrigen Compiler-/Analyserdiagnosen bleiben aktiv.

Blumind steht unter MIT-Lizenz, siehe `LICENSE`. Die Hinweise für enthaltene Icons und Abhängigkeiten bleiben erhalten. Die lokale Testversion ist weder signiert noch extern veröffentlicht.
