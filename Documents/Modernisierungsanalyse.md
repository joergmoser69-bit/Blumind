# Blumind: Codeanalyse und Modernisierung

Stand: 2. Oktober 2026. Untersucht wurde der lokale Stand mit HEAD `bec8d56`.

## Empfehlung

**Die vorhandene Basis ist für eine weiterentwickelte Windows-Anwendung brauchbar. Ich empfehle eine schrittweise Modernisierung.** Das vorhandene Verhalten, die Tastaturbedienung und die Kompatibilität mit alten Mindmaps sollten dabei erhalten bleiben.

Für eine Anwendung für macOS, Linux oder den Browser wäre eine neue Oberfläche erforderlich. Auch dann lohnt sich eine gezielte Übernahme von Dateiformat, Fachmodell, Layoutalgorithmen und Befehlslogik. Diese Teile sind allerdings noch nicht unabhängig von Windows und der Oberfläche.

Eine komplette Neuentwicklung würde zahlreiche bereits vorhandene Funktionen erneut erfordern: Auswahl und Mehrfachauswahl, Drag-and-drop, Zoom, Navigation, Layout, Widgets, Undo/Redo, Drucken, Import/Export und alte Dateiformate. Allein das Alter der Technologien rechtfertigt diesen Aufwand nicht.

## Umfang und Grenzen der Untersuchung

- 464 C#-Dateien mit ungefähr 85.845 Zeilen einschließlich Leerzeilen, Kommentaren und generiertem Code. 451 C#-Dateien stehen in der Projektdatei. Diese Zahlen sind eine Größenordnung, keine Qualitätsmetrik.
- Ein C#-Anwendungsprojekt sowie ein separates Projekt für native Windows-Ressourcen; keine eigentliche C++-Fachlogik in diesem Ressourcenprojekt.
- Letzter Commit der lokalen Historie: 27. Juli 2017. Daraus folgt nicht, dass außerhalb dieses Repositorys keine späteren Versionen existieren.
- Vertieft untersucht: Projekt/Build, Start, Dokumentladen und -speichern, Modell, Layout, Grafikabstraktion, Befehle/Undo/Redo, Notizeditor, Update-Prüfung und Verpackung.
- Keine automatisierte Testsuite und keine CI-Konfiguration im Repository gefunden. `TestForm.cs` ist eine manuelle Testoberfläche.
- Alle in der Projektdatei aufgeführten Compile-/Content-/EmbeddedResource-/None-Dateien waren vorhanden.
- Ein Release-Build des C#-Projekts mit dem vorhandenen Framework-MSBuild scheiterte an `MSB3091`: `resgen.exe` beziehungsweise die alten Windows-SDK-Werkzeuge fehlen. Ein vorheriger Versuch innerhalb der Sandbox scheiterte schon an einem SDK-Verzeichniszugriff; mit erlaubtem Zugriff zeigte sich die fehlende Werkzeugabhängigkeit.
- Ein .NET-10-SDK ist vorhanden. Das vorhandene Projekt wurde aber nicht auf .NET 10 portiert. Kein erfolgreicher Build, kein interaktiver Anwendungstest, keine Performance-Messung und kein Nachweis einer ausnutzbaren Sicherheitslücke.
- Der Anwendungsquellcode wurde für diese Analyse nicht verändert. Build-Zwischendateien sind durch die vorhandene `.gitignore` abgedeckt.

## Was sich erhalten lässt

**Dokumentenmodell und Kompatibilität:** `Document`, `MindMap`, `Topic`, `Link` und Widgets bilden die Domäne bereits erkennbar ab. `.bmd` ist lesbares XML mit Versionskennung. Der Loader unterscheidet ältere Ein-Karten-Dateien und neuere Dokumente mit mehreren Karten. Ein Formatwechsel zu JSON ist für die Modernisierung nicht erforderlich.

**Befehlsmodell:** Änderungen sind vielfach als eigene Commands mit `Execute()` und `Rollback()` modelliert. Das ist eine gute Grundlage für zuverlässiges Undo/Redo und gezielte Tests, auch wenn die Verwaltung der Historie korrigiert werden sollte.

**Layout:** Separate Layouter für Mindmap-, Baum-, Organisations- und Logikdarstellungen enthalten wiederverwendbares Wissen über die Anordnung von Knoten und Verbindungen.

**Grafik und Export:** `Canvas/IGraphics.cs` und Implementierungen für GDI+, PDF und SVG schaffen bereits eine Austauschstelle für Zeichenoperationen. Die Schnittstellen verwenden noch zahlreiche `System.Drawing`-Typen; sie sind daher keine vollständig plattformunabhängige Renderbibliothek.

**Anwendungsfunktionen:** Mehrere Exportformate, FreeMind-Import, Übersetzungen, Themes und portable Betriebsweise sind vorhanden. Das spricht für eine gezielte Weiterentwicklung eines umfangreichen Werkzeugs.

## Technologien und sinnvolle Zielrichtung

| Bereich | Befund | Empfehlung |
|---|---|---|
| Laufzeit | `.NET Framework 2.0`, auch in `app.config` | Ziel: .NET 10 LTS, `net10.0-windows` |
| Oberfläche | Windows Forms mit vielen eigenen Controls | Für Windows zunächst erhalten; DPI, Kontrast, Tastaturbedienung und Darstellung gezielt verbessern |
| Projekt | Altes MSBuild-Format mit einzelnen Dateieinträgen | SDK-Projekt, definierte SDK-Version, dokumentierter Build |
| Kompatibilitätsschicht | Eigenes LINQ, `Func` und `ExtensionAttribute` für alte Laufzeit | Durch Standardimplementierungen ersetzen und Unterschiede testen |
| Notizeditor | WinForms `WebBrowser`, DOM-/Script-Zugriffe | Eigenständiges Arbeitspaket: WebView2 mit kompatiblem HTML-Editor oder einfacher Rich-Text-/Markdown-Editor |
| PDF | Mitgelieferte `PdfSharp.dll`, Dateiversion `1.32.2608.0` | Gepflegtes PDFsharp über NuGet; GDI-Variante als naheliegende Windows-Option evaluieren |
| Nebenläufigkeit | `Thread`, `WebClient`, `Thread.Abort`, wartende UI-Schleifen | `HttpClient`, `async`/`await`, Abbruch über `CancellationToken` |
| Web-Hilfsfunktionen | `System.Web.HttpUtility.HtmlDecode` | Abhängigkeit entfernen; z. B. `System.Net.WebUtility.HtmlDecode` und Verhalten prüfen |
| Verpackung | NSIS, ILMerge, Batchdateien mit lokalen Pfaden | Reproduzierbares Publish und portable Ausgabe; Installer nach tatsächlichem Bedarf |
| Native Ressourcen | Ressourcenprojekt mit VS-2012-Toolset `v110` | Prüfen, was weiter benötigt wird; Ressourcen/Manifest reproduzierbar bauen |

Windows Forms ist weiterhin unterstützt und erhält Updates. Ein Wechsel auf WPF oder WinUI 3 ist keine Voraussetzung für eine zeitgemäße Windows-Anwendung. Microsoft beschreibt ausdrücklich die Modernisierung auf aktuelles .NET bei Beibehaltung des UI-Frameworks. [Microsoft: Migrationsentscheidung](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/migration-decision-guide)

.NET 10 ist die aktuelle LTS-Zielrichtung mit Support bis November 2028. .NET 8 wäre im Oktober 2026 wegen seines unmittelbar bevorstehenden Supportendes keine gute neue Ausgangsbasis. [Microsoft: Support Policy](https://dotnet.microsoft.com/en-us/platform/support/policy)

Eine Zwischenstufe auf .NET Framework 4.8/4.8.1 kann sinnvoll sein, falls sie schneller einen prüfbaren Ausgangsstand liefert. Sie sollte eine begründete Hilfe bei der Migration sein; der direkte Weg zu .NET 10 ist ebenfalls zu prüfen.

## Konkrete Befunde mit Prioritäten

### 1. Hoch: Originaldatei beim Speichern gefährdet

In [Document.IO.cs](C:/Repos2/Blumind/Code/Blumind/Model/Documents/Document.IO.cs:238) öffnet `Save(string)` die Zieldatei mit `FileMode.Create`. Die XML-Struktur wird vorher erzeugt, aber die bestehende Datei wird beim Öffnen abgeschnitten. Ein Fehler während des Schreibens kann die bisherige Version zerstören.

**Verbesserung:** In eine temporäre Datei im selben Verzeichnis schreiben, vollständig abschließen und die Zieldatei anschließend ersetzen. Backup und Wiederherstellung ergänzen. Den Umgang mit neuen Dateien, fehlenden Schreibrechten und fehlgeschlagenem Ersetzen explizit behandeln. Keine automatische Wiederherstellungsspeicherung im untersuchten C#-Code gefunden.

### 2. Hoch: Unsichere und veraltete Update-Prüfung

[CheckUpdate.cs](C:/Repos2/Blumind/Code/Blumind/Dialogs/CheckUpdate.cs:28) lädt Versionsinformationen über HTTP. Eine Zeile der Antwort wird als Downloadadresse übernommen und nach Klick geöffnet. Eine automatische Installation findet in diesem Code nicht statt, dennoch kann manipulierte Updateinformation zu einer falschen Downloadadresse führen. Die aktuelle Erreichbarkeit oder Vertrauenswürdigkeit der alten Domain wurde nicht geprüft.

**Verbesserung:** Alten Updater für einen ersten modernisierten Stand deaktivieren oder auf eine definierte, vertrauenswürdige HTTPS-Quelle umstellen. Adresse/Schemata validieren und Netzwerkfehler von „kein Update vorhanden“ unterscheiden. `Thread.Abort()` durch kooperativen Abbruch ersetzen; unter modernem .NET wirft es eine `PlatformNotSupportedException`. [Microsoft: Thread.Abort](https://learn.microsoft.com/en-us/dotnet/api/system.threading.thread.abort?view=net-10.0)

### 3. Hoch: Dokument- und HTML-Verarbeitung härten

[Document.IO.cs](C:/Repos2/Blumind/Code/Blumind/Model/Documents/Document.IO.cs:84) verwendet `new XmlDocument().Load(stream)` ohne explizite Regeln für DTDs, externe Ressourcen oder Größenbegrenzungen. Weitere XML-Einstiegspunkte existieren bei Import und Zwischenablage. Für die alte Laufzeit sollte man sich nicht auf sichere Standardwerte verlassen.

**Verbesserung:** Zentralen XML-Ladeweg mit deaktivierter Auflösung externer Ressourcen, gesperrten DTDs sowie Grenzen für Dokumentgröße und Verschachtelung einführen. Kaputte Dokumente müssen verständliche Fehlermeldungen liefern. Die konkrete Ausnutzbarkeit auf der ursprünglichen Laufzeit wurde nicht getestet. [Microsoft: XML-Resolver](https://learn.microsoft.com/en-us/dotnet/api/system.xml.xmldocument.xmlresolver?view=netframework-4.8.1), [DTD-Verarbeitung](https://learn.microsoft.com/en-us/dotnet/api/system.xml.xmlreadersettings.dtdprocessing?view=net-10.0)

Im [HtmlEditBox](C:/Repos2/Blumind/Code/Blumind/Controls/HtmlEditors/HtmlEditBox.cs:105) wird Inhalt direkt als `InnerHtml` eingesetzt. Es gibt Regex-Filter, diese werden in den untersuchten Gettern angewandt; das Einsetzen des Inhalts verwendet sie dort nicht. Bei einem neuen Editor braucht es eine ausdrücklich festgelegte Behandlung von Scripts, Eventhandlern, externen Bildern und Links. WebView2 allein übernimmt diese Aufgabe nicht.

### 4. Hoch für die Migration: Alter Notizeditor und UI-Warteverhalten

[HtmlEditBox.cs](C:/Repos2/Blumind/Code/Blumind/Controls/HtmlEditors/HtmlEditBox.cs:153) verwendet den Internet-Explorer-basierten `WebBrowser`. `WaitUntilBrowserReady()` wartet bis zu sechs Sekunden mit `Thread.Sleep` und `Application.DoEvents`; ein weiterer Pfad enthält eine unbeschränkte Warteschleife. Daraus ergeben sich Risiken für blockierte oder wiedereintretende UI-Abläufe, ohne dass hier ein konkreter Hänger reproduziert wurde.

**Verbesserung:** Asynchrone Initialisierung und klarer Editor-Lebenszyklus. Bei WebView2 müssen die bisherigen DOM-/Script-Aufrufe angepasst und das Editorverhalten neu integriert werden. Das ist kein einfacher Austausch des Control-Namens. Vorhandene HTML-Notizen müssen weiterhin lesbar bleiben. [Microsoft: WebView2 für WinForms](https://learn.microsoft.com/en-us/microsoft-edge/webview2/get-started/winforms)

### 5. Mittel bis hoch: Undo/Redo meldet Fehler nicht korrekt

In [ChartControl.cs](C:/Repos2/Blumind/Code/Blumind/ChartControls/ChartControl.cs:105) liefert `ExecuteCommand()` auch dann `true`, wenn `command.Execute()` `false` zurückgibt. `Undo()` und `Redo()` verschieben Commands zwischen den Stacks, ohne die booleschen Ergebnisse von `Rollback()` beziehungsweise `Execute()` zu berücksichtigen. Bei Ausnahmen ist der Command bereits aus dem ursprünglichen Stack entfernt.

**Verbesserung:** Historie erst nach erfolgreicher Operation ändern; Fehlerstatus korrekt weiterreichen; Verhalten bei Ausnahmen definieren. Außerdem den gespeicherten Dokumentzustand für die Änderungsanzeige verfolgen und eine sinnvolle Grenze für die Historie vorsehen. Nicht pauschal neue Architektur einführen, sondern zuerst die vorhandene Befehlslogik absichern.

### 6. Mittel: Grafikressourcen werden nicht zuverlässig freigegeben

[MindMapView.Paint.cs](C:/Repos2/Blumind/Code/Blumind/ChartControls/MindMap/MindMapView/MindMapView.Paint.cs:36) erzeugt beim Zeichnen einen `Pen` ohne `Dispose`. Auch bei Chart-Labels werden Brushes direkt erzeugt. `GdiPen` und `GdiBrush` verpacken native Grafikressourcen, bieten aber selbst keinen Freigabevertrag.

**Verbesserung:** Eigentum an Grafikobjekten festlegen, temporäre Objekte mit `using` freigeben, Wrapper-Lebensdauer klären. Unter wiederholtem Zeichnen GDI-Handlezahl und Speicher messen. Eine tatsächliche Ressourcenerschöpfung wurde hier nicht gemessen.

### 7. Mittel: Fachlogik noch eng an Windows und globale Zustände gebunden

`Topic` verbindet Baumstruktur, Styles, UI-Eigenschaften, Widgets und XML-Serialisierung. [Layouter.cs](C:/Repos2/Blumind/Code/Blumind/ChartControls/MindMap/Layouts/Layouter.cs:124) verwendet WinForms-Textmessung und globale Optionen. Weitere zentrale globale Zustände sind `Program.MainForm` und `Options.Current`.

**Verbesserung:** Schrittweise Fachmodell, Dateiverarbeitung, Layout, Darstellung und UI trennen. Textmessung und benötigte Optionen als Abhängigkeiten übergeben. Das ermöglicht Tests und eine spätere neue Oberfläche. Die vorhandenen Ordner allein ergeben noch keine getrennten Bibliotheken.

### 8. Mittel: DPI, Packaging und unvollständige Funktionen

Das Manifest enthält keine DPI-Deklaration; viele Maße sind feste Pixelwerte. Die Formulare verwenden zugleich Font-Autoskalierung. Deshalb sind Skalierungsprobleme zu erwarten und gezielt zu prüfen, nicht bereits als gemessener Fehler festzustellen.

Die Verpackungsskripte enthalten absolute Werkzeugpfade und Verweise auf nicht vorhandene Verzeichnisse wie `Blumind Online`. NSIS selbst muss deshalb nicht ersetzt werden; das Problem ist der nicht reproduzierbare Ablauf.

`FlowDiagram` ist ein Gerüst. Der Dokumentloader verarbeitet nur Mindmaps und überspringt nicht unterstützte Diagrammtypen. Solche Typen sollten entweder vollständig unterstützt oder mit klarer Fehlermeldung abgelehnt werden, damit ein anschließendes Speichern keinen stillen Verlust erzeugt.

**Verbesserung:** DPI-Tests bei 100/150/200 Prozent und beim Monitorwechsel; eigene Controls auf Kontrast und Tastaturzugänglichkeit prüfen. Portable Ausgabe zuerst automatisieren. Win32-Schnittstellen und unsafe-Bildroutinen auf x64 prüfen; ein 64-Bit-Fehler wurde in dieser Analyse nicht nachgewiesen.

## Vorgeschlagener Fahrplan

### Etappe 1: Einen prüfbaren Ausgangsstand herstellen

Build auf einer dokumentierten Umgebung herstellen; alte Beispielkarten als Kompatibilitätsfälle sichern; die wichtigsten Bedienabläufe festhalten. Dann gezielte Regressionstests für Laden/Speichern und Befehle ergänzen. Keine Installation alter SDKs nur aufgrund der Fehlermeldung, bevor der moderne Buildweg geprüft wurde.

**Abnahme:** Reproduzierbarer Build; alte und neue `.bmd`-Beispiele öffnen; Speichern und erneutes Laden erhält Inhalte, Links, Widgets und mehrere Karten.

### Etappe 2: Zuverlässigkeit verbessern

Sicheres Speichern und Backup, XML-Grenzen, Update-Verhalten, Fehlerbehandlung, Undo/Redo und Grafikressourcen korrigieren. Sicherheits- und Datenverlusttests erhalten Vorrang vor kosmetischen Änderungen.

**Abnahme:** Fehlgeschlagener Schreibvorgang beschädigt die bestehende Karte nicht; ungültige Imports werden kontrolliert abgelehnt; fehlgeschlagene Befehle verändern die Historie nicht.

### Etappe 3: Auf .NET 10 migrieren

SDK-Projekt anlegen, alte Kompatibilitätsklassen entfernen, Bibliotheken über NuGet beziehen, nicht verfügbare Framework-APIs ersetzen und die Ressourcenpipeline erneuern. Windows Forms zunächst erhalten. Den Notizeditor als separates Arbeitspaket integrieren.

**Abnahme:** Öffnen, Editieren, Drag-and-drop, Undo/Redo, Notizen, PDF/SVG/PNG-Export und Druck funktionieren auf der neuen Laufzeit; portable Ausgabe funktioniert auf einem Testsystem ohne Entwicklungswerkzeuge.

### Etappe 4: Im Alltag spürbar verbessern

Priorität für Autosave/Wiederherstellung, scharfe Darstellung auf hochauflösenden Monitoren, verlässliche Exporte und flüssiges Arbeiten mit großen Karten. Danach Such-/Filterfunktionen ausbauen, optional Markdown-Notizen und Dark Mode. Suche, Themes und Tastaturkürzel sind bereits vorhanden und sollten erweitert werden.

**Abnahme:** Vergleich mit dem Ausgangsstand anhand gleicher Karten und Bedienabläufe. Performance erst messen, dann gezielt optimieren; aus der Codelektüre lässt sich keine belastbare maximale Kartengröße ableiten.

## Wann eine neue Oberfläche sinnvoll wird

| Ziel | Einschätzung |
|---|---|
| Vertrautes, schnelles Windows-Werkzeug erhalten | Bestehende Anwendung modernisieren |
| Windows-Oberfläche deutlich verändern | Kern schrittweise trennen; neue Oberfläche später anhand konkreter Anforderungen evaluieren |
| Windows, macOS und Linux | Neue Oberfläche und Renderintegration; Fachlogik und Dateiformat selektiv übernehmen |
| Browser, Zusammenarbeit in Echtzeit, Cloud-Synchronisierung als Hauptzweck | Weitgehende Neuentwicklung; Bestand als Verhaltensreferenz und Quelle für Import/Layout verwenden |

Ein passendes neues UI-Framework sollte erst nach Festlegung der Plattformen gewählt werden. WPF, WinUI, plattformübergreifende Desktop-Frameworks und Webtechnologien lösen unterschiedliche Aufgaben; ein Wechsel allein wegen eines moderneren Erscheinungsbilds ist hier nicht begründet.

## Offene Entscheidungen

Vor einer umfangreichen Umsetzung: Bleibt Windows die einzige Zielplattform? Welche drei Alltagsprobleme sollen zuerst gelöst werden? Wie wichtig sind HTML-Notizen und die kleine portable Ausgabe? Muss eine neue Datei auch in der ursprünglichen Blumind-Version bearbeitbar bleiben?

Eine belastbare Aufwandsschätzung braucht zunächst einen erfolgreichen Build und eine Probe der besonders alten Komponenten. Die Analyse belegt überschaubare Einstiegspunkte, aber keine risikofreie Vollmigration durch bloßes Ändern der Frameworkversion.

Die Hauptquellen stehen unter MIT-Lizenz. Bei Weitergabe sind die vorhandenen Copyright-/Lizenzhinweise sowie separate Hinweise und Lizenzbedingungen für PDFsharp, Icons und eingebundene Fremdkomponenten zu berücksichtigen.

PDFsharp hat aktuelle NuGet-Varianten einschließlich einer GDI-Ausgabe für Windows. Beim Austausch sind APIs, Fontauflösung und Exportdarstellung zu prüfen. [PDFsharp: Pakete](https://docs.pdfsharp.net/General/Overview/NuGet-Packages.html), [PDFsharp: Änderungen in 6.x](https://docs.pdfsharp.net/General/Overview/Whats-New.html)
