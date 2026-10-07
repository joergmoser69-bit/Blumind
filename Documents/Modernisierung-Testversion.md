# Blumind .NET 10 – erste Testversion

Stand: 2. Oktober 2026. Ziel ist die technische Modernisierung bei vertrauter Bedienung. Oberfläche, Mindmap-Struktur und `.bmd`-Dateiformat bleiben erhalten.

## Starten

Die ZIP-Datei vollständig in einen beschreibbaren Ordner entpacken und `Start-Portable.cmd` starten. Die Ausgabe enthält die .NET-10-Laufzeit für Windows x64; eine zusätzliche .NET-Installation ist nicht nötig. Einstellungen werden im portablen Ordner abgelegt.

Für formatierte Notizen wird die Edge WebView2 Runtime benötigt. Bei fehlender Runtime erscheint ein HTML-Quelltexteditor. Die alte Installation kann parallel bestehen bleiben; Dateien werden nur an eine bereits laufende Instanz derselben ausführbaren Datei weitergereicht.

## Was geändert wurde

- SDK-Projekte und .NET 10 statt .NET Framework 2.0; die aktuellen NuGet-Versionen sind festgelegt und gesperrt.
- PDFsharp 6.2.4 über NuGet statt der alten mitgelieferten DLL.
- WebView2 statt Internet Explorer für Notizen, mit asynchroner Initialisierung, lokalem Editor und Behandlung unsicherer HTML-Inhalte.
- XML für die Mindmap-Zwischenablage; kompatibles Kopieren von Knoten einschließlich Hierarchie und Formatierung innerhalb der neuen Version. Zwischen alter und neuer Version funktioniert zumindest der Textweg; der alte binäre Objektweg wird nicht verwendet.
- Sicheres Speichern über eine temporäre Datei und anschließendes Ersetzen; bei Überschreiben wird die vorherige Version als `.bmd.bak` erhalten.
- Begrenzte XML-Dokumentgröße (64 MiB Zeichen), begrenzte Verschachtelung (256 Ebenen) und gesperrte externe DTDs/Entities. Unbekannte oder nicht unterstützte Diagrammtypen werden mit einer Fehlermeldung abgewiesen.
- Undo/Redo erhält seine Historie auch bei fehlgeschlagenen Befehlen.
- Grafikressourcen werden nach Zeichen-/Exportvorgängen freigegeben; eingebettete unveränderte Bilder behalten ihre ursprünglichen Bytes.
- Windows-DPI-Modus, Manifest und Shell-Aufrufe für die neue Laufzeit angepasst.
- Die alte HTTP-Update-Prüfung ist deaktiviert. Es gibt noch keine neue Release-/Updatequelle.

## Bitte im Alltag prüfen

Automatisch geprüft: Release-Build ohne Warnungen/Fehler, 22 erfolgreiche Regressionstests und erfolgreicher Start der portablen EXE. Die Prüfungen umfassen fünf bestehende `.bmd`-Beispiele, drei FreeMind-Importe, Speichern/Wiederöffnen, Bilddaten, Undo/Redo, XML-Zwischenablage, PNG/SVG/PDF und den WebView2-Notizeditor einschließlich fortlaufender Eingabe im Notizfeld.

1. Eine eigene Karte unter einem neuen Namen öffnen und speichern; anschließend schließen und erneut öffnen.
2. Knoten hinzufügen, umbenennen, verschieben, löschen sowie Undo/Redo und Kopieren/Einfügen verwenden.
3. Mehrere Karten, Verbindungen, Bilder, Fortschrittsanzeigen und vorhandene HTML-Notizen prüfen.
4. Notizen bearbeiten, Änderungen übernehmen und zwischen Quelltext-/Formatansicht wechseln.
5. PNG, SVG und PDF exportieren; die Dateien öffnen und Darstellung sowie Schriften vergleichen.
6. Zoom, Suche, Tastaturkürzel, Druckvorschau und das Verhalten auf deinen Monitoren ausprobieren.

Rückmeldungen sind besonders hilfreich mit: ausgeführtem Schritt, erwartetem Verhalten, tatsächlichem Verhalten und einer Beispielkarte, falls der Fehler vom Inhalt abhängt.

## Grenzen dieses Schritts

- Autosave/Wiederherstellung, Dark Mode, eine neue Oberfläche und plattformübergreifende Unterstützung sind noch nicht umgesetzt.
- Die automatisierten Prüfungen ersetzen keinen Vergleich auf verschiedenen Monitoren oder mit einem echten Drucker.
- Externe Bilder in HTML-Notizen werden nicht automatisch aus dem Netz geladen. Neue Notizbilder lassen sich aus einer lokalen Datei einbetten. Web-/Mail-Links sind erlaubt; aktive Inhalte und lokale Dateilinks werden bei der formatierten Bearbeitung entfernt. Unverändert angezeigte Notizen behalten ihren gespeicherten Quelltext.
- Die Browser-Bearbeitungsbefehle verwenden derzeit Chromium-`execCommand`; das ist ein Übergang für die bisherige Formatierungsleiste, kein eigener moderner Rich-Text-Editor.
- Bilddownloads verwenden HttpClient mit Zeit-/Größenlimit, bleiben im bestehenden synchronen Aufrufpfad. Große Karten wurden noch nicht systematisch vermessen.
- Das native Dokument-Icon wird im neuen Build als separate `document.ico` mitgeliefert. Dateizuordnung wird nicht automatisch eingerichtet.
- Das Paket ist eine lokale, nicht signierte Testversion.

Build und Prüfungen lassen sich mit `scripts/Build.ps1` reproduzieren. Die Originaldateien der mitgelieferten Beispiele bleiben unverändert.

Lizenztexte für mitgelieferte Abhängigkeiten liegen im Paket unter `Licenses/`. Die PDFsharp-Lizenz stammt aus dem [Quellcode der Version 6.2.4](https://github.com/empira/PDFsharp/blob/v6.2.4/LICENSE).
