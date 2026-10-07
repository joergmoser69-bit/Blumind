using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Xml;
using Blumind;
using Blumind.Canvas.Pdf;
using Blumind.Canvas.Svg;
using Blumind.Configuration;
using Blumind.Controls;
using Blumind.Controls.MapViews;
using Blumind.Core;
using Blumind.Globalization;
using Blumind.Model;
using Blumind.Model.Documents;
using Blumind.Model.MindMaps;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

[assembly: SupportedOSPlatform("windows10.0.17763")]

static class RegressionTests
{
    static int failures, passed;
    static string root, output;

    [STAThread]
    static int Main(string[] args)
    {
        root = Path.GetFullPath(args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        output = Path.Combine(root, "artifacts", "tests");
        Directory.CreateDirectory(output);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        ProgramEnvironment.RunMode = ProgramRunMode.Portable;
        Options.Current.SetValue(OptionNames.Miscellaneous.SaveTabs, SaveTabsType.No);
        UIColorThemeManage.Initialize();
        LanguageManage.Initialize();
        typeof(Blumind.Program).GetProperty("IsRunTime").SetValue(null, true);

        foreach (string file in Directory.GetFiles(Path.Combine(root, "Documents"), "*.bmd"))
            Run("BMD roundtrip: " + Path.GetFileName(file), () => Roundtrip(file));
        foreach (string file in Directory.GetFiles(Path.Combine(root, "Documents", "FreeMind"), "*.mm"))
            Run("FreeMind import: " + Path.GetFileName(file), () =>
            {
                var doc = FreeMindFile.LoadFile(file);
                Check(doc?.Charts.Count > 0, "No imported chart");
                doc.Save(Path.Combine(output, Path.GetFileName(file) + ".bmd"));
            });

        Run("Atomic save, backup and write failure", TestAtomicFile);
        Run("Embedded PNG preserves bytes and detects pixel edits", () =>
        {
            using var bitmap = new Bitmap(4, 4);
            bitmap.SetPixel(1, 1, Color.FromArgb(253, 232, 166, 166));
            string original = ST.ImageBase64String(bitmap);
            var xml = XmlIO.Parse("<image><![CDATA[" + original + "]]></image>");
            using var decoded = (Bitmap)ST.ReadImageNode(xml.DocumentElement);
            Check(ST.ImageBase64String(decoded) == original, "Unchanged image re-encoded");
            decoded.SetPixel(0, 0, Color.Blue);
            Check(ST.ImageBase64String(decoded) != original, "Edited image reused stale bytes");
        });
        Run("XML DTD rejected", () => Throws<XmlException>(() => XmlIO.Parse("<!DOCTYPE x [<!ENTITY y SYSTEM 'file:///never-read'>]><x>&y;</x>")));
        Run("XML depth limit", () => Throws<XmlException>(() => XmlIO.Parse(string.Concat(Enumerable.Repeat("<x>", 260)) + string.Concat(Enumerable.Repeat("</x>", 260)))));
        Run("Unsupported and unknown charts rejected", () =>
        {
            foreach (string type in new[] { "FlowDiagram", "Unknown", "999" })
                Throws<NotSupportedException>(() => Document.Load(XmlIO.Parse("<document><charts><chart type='" + type + "'/></charts></document>")));
        });
        Run("Caller retains ownership of input stream", () =>
        {
            using var stream = File.OpenRead(Directory.GetFiles(Path.Combine(root, "Documents"), "*.bmd")[0]);
            Document.Load(stream);
            Check(stream.CanRead, "Input stream was closed");
        });
        Run("Undo/redo failure preserves history", TestCommands);
        Run("Clipboard XML preserves hierarchy and Unicode", () =>
        {
            var topic = new Topic("ä 日本語 🌱");
            topic.Children.Add(new Topic("child"));
            var xml = new MapClipboardData(new ChartObject[] { topic }).Data;
            var restored = new MapClipboardData(xml).GetTopics();
            Check(restored.Length == 1 && restored[0].Text == topic.Text && restored[0].Children.Count == 1, "Clipboard data changed");
        });
        Run("Layout and PNG/SVG/PDF rendering", TestExports);
        Run("External link scheme validation", () =>
        {
            Check(HtmlEditBox.IsAllowedLink("https://example.org"), "HTTPS rejected");
            Check(!HtmlEditBox.IsAllowedLink("javascript:alert(1)") && !HtmlEditBox.IsAllowedLink("file:///C:/test.exe"), "Unsafe link accepted");
        });

        using var host = new Form { ShowInTaskbar = false, Opacity = 0, Width = 900, Height = 650 };
        HtmlEditBox.UserDataFolder = Path.Combine(output, "webview-profile");
        using var editor = new HtmlEditBox { Dock = DockStyle.Fill, Text = "<p><b>Existing HTML</b> ä</p>" };
        host.Controls.Add(editor);
        host.Shown += async (_, _) =>
        {
            try
            {
                await RunAsync("WebView2 initialization and unchanged note", async () =>
                {
                    await editor.EnsureReadyAsync();
                    Check(editor.Browser.CoreWebView2 != null, "WebView2 runtime unavailable");
                    Check(await editor.Browser.CoreWebView2.ExecuteScriptAsync("typeof window.editor.set") == "\"function\"", "Editor script did not initialize");
                    await editor.EndEditAsync();
                    Check(editor.Text == "<p><b>Existing HTML</b> ä</p>", "Displaying a note changed stored HTML");
                });
                if (editor.Browser.CoreWebView2 != null)
                {
                    await RunAsync("HTML sanitizer and formatted editing", async () =>
                    {
                        string script = "(() => { try { return window.editor.sanitize(" + JsonSerializer.Serialize("<p onclick='alert(1)'><b>safe</b><script>alert(1)</script><a href='javascript:alert(1)'>x</a></p>") + "); } catch(e) { return 'ERROR: '+e.stack; } })()";
                        string sanitized = JsonSerializer.Deserialize<string>(await editor.Browser.CoreWebView2.ExecuteScriptAsync(script));
                        Check(sanitized != null && sanitized.Contains("<b>safe</b>") && !sanitized.Contains("script") && !sanitized.Contains("onclick"), "Sanitizer failed: " + sanitized);
                        await editor.Browser.CoreWebView2.ExecuteScriptAsync("window.editor.command('selectAll'); window.editor.command('insertText','Edited ä 日本語');");
                        await editor.EndEditAsync();
                        Check(editor.Text.Contains("Edited ä 日本語"), "Last editor change missing");
                        editor.Text = "<p>Second note</p>";
                        await editor.Browser.CoreWebView2.ExecuteScriptAsync("true");
                        await editor.EndEditAsync();
                        Check(editor.Text == "<p>Second note</p>", "Stale editor message overwrote the next note");
                    });
                }
                await RunAsync("Inline note editing retains selection and editor state", async () =>
                {
                    var topic = new Topic("Note") { Remark = "<p>Original</p>" };
                    using var remark = new RemarkEditor { Dock = DockStyle.Fill, CurrentObject = topic };
                    host.Controls.Add(remark);
                    remark.BringToFront();
                    typeof(RemarkEditor).GetProperty("EditMode").SetValue(remark, true);
                    var html = remark.Controls.OfType<HtmlEditor>().Single();
                    await html.EditBox.EnsureReadyAsync();
                    var browser = html.EditBox.Browser.CoreWebView2;
                    Check(browser != null, "Note browser unavailable");
                    int version = JsonDocument.Parse(await browser.ExecuteScriptAsync("window.editor.state()")).RootElement.GetProperty("revision").GetInt32();
                    await browser.ExecuteScriptAsync("window.editor.command('selectAll'); window.editor.command('insertText','First');");
                    await Task.Delay(150);
                    await browser.ExecuteScriptAsync("window.editor.command('insertText',' Second');");
                    await html.EndEditAsync();
                    using var state = JsonDocument.Parse(await browser.ExecuteScriptAsync("window.editor.state()"));
                    Check(topic.Remark.Contains("First Second"), "Typing lost text or selection: " + topic.Remark);
                    Check(state.RootElement.GetProperty("revision").GetInt32() == version, "Own model notification reloaded the editor");
                });
                await RunAsync("Main window opens an existing document", async () =>
                {
                    using var main = new MainForm(Array.Empty<string>()) { Opacity = 0, ShowInTaskbar = false };
                    typeof(Blumind.Program).GetProperty("MainForm").SetValue(null, main);
                    main.Show();
                    main.OpenDocument(Path.Combine(root, "Documents", "Blumind Quick Help.bmd"));
                    await Task.Delay(800);
                    Check(main.IsHandleCreated && !main.IsDisposed, "Main window not running");
                    Check(main.GetForms<DocumentForm>().Any(f => f.Document != null), "Document view not opened");
                    foreach (var form in main.GetForms<DocumentForm>()) form.Document.Modified = false;
                    main.Close();
                });
            }
            catch (Exception e) { failures++; Console.WriteLine("FAIL UI runner: " + e); }
            finally { host.Close(); }
        };
        Application.Run(host);
        Console.WriteLine($"RESULT: {passed} passed, {failures} failed.");
        return failures == 0 ? 0 : 1;
    }

    static void Roundtrip(string file)
    {
        var document = Document.Load(file);
        Check(document?.Charts.Count > 0, "No chart loaded");
        Check(!document.Modified, "Loaded document unexpectedly marked modified");
        string first = Path.Combine(output, Path.GetFileName(file));
        string second = first + ".roundtrip.bmd";
        var before = document.Charts.OfType<MindMap>().SelectMany(m => m.GetTopics(true)).Select(t => t.Text).ToArray();
        document.Save(first);
        var reloaded = Document.Load(first);
        Check(before.SequenceEqual(reloaded.Charts.OfType<MindMap>().SelectMany(m => m.GetTopics(true)).Select(t => t.Text)), "Topic text changed");
        reloaded.Save(second);
        var firstXml = XmlIO.Load(first);
        var secondXml = XmlIO.Load(second);
        // GDI+ may re-encode PNG metadata; verify decoded pixels rather than compressed bytes.
        var firstThumbs = firstXml.SelectNodes("//thumb");
        var secondThumbs = secondXml.SelectNodes("//thumb");
        Check(firstThumbs.Count == secondThumbs.Count, "Image count changed");
        for (int index = 0; index < firstThumbs.Count; index++)
        {
            using var aStream = new MemoryStream(Convert.FromBase64String(firstThumbs[index].InnerText));
            using var bStream = new MemoryStream(Convert.FromBase64String(secondThumbs[index].InnerText));
            using var a = new Bitmap(aStream);
            using var b = new Bitmap(bStream);
            Check(a.Size == b.Size, "Image dimensions changed");
            for (int y = 0; y < a.Height; y++)
                for (int x = 0; x < a.Width; x++)
                    Check(a.GetPixel(x, y) == b.GetPixel(x, y), $"Image pixels changed at {x},{y}: {a.GetPixel(x, y)} -> {b.GetPixel(x, y)}");
            firstThumbs[index].InnerText = secondThumbs[index].InnerText = "verified-image";
        }
        Check(firstXml.OuterXml == secondXml.OuterXml, "Persisted content changed after reload");
    }

    static void TestAtomicFile()
    {
        string file = Path.Combine(output, "atomic.txt");
        File.WriteAllText(file, "original");
        Throws<IOException>(() => AtomicFile.Write(file, s => { s.WriteByte(42); throw new IOException("Simulated write failure"); }));
        Check(File.ReadAllText(file) == "original", "Existing file damaged");
        Check(!Directory.GetFiles(output, ".atomic.txt.*.tmp").Any(), "Temporary file left behind");
        AtomicFile.Write(file, s => s.Write(System.Text.Encoding.UTF8.GetBytes("new")));
        Check(File.ReadAllText(file) == "new" && File.ReadAllText(file + ".bak") == "original", "Replacement/backup failed");
    }

    static void TestCommands()
    {
        using var view = new MindMapView { Map = new MindMap("Commands") };
        var command = new ProbeCommand { ExecuteResult = false };
        Check(!view.ExecuteCommand(command) && !view.CanUndo, "Failed command recorded");
        command.ExecuteResult = true;
        Check(view.ExecuteCommand(command) && view.CanUndo, "Successful command missing");
        command.RollbackResult = false;
        view.Undo();
        Check(view.CanUndo && !view.CanRedo, "Failed rollback changed history");
        command.RollbackResult = true;
        view.Undo();
        Check(!view.CanUndo && view.CanRedo, "Undo failed");
        command.ExecuteResult = false;
        view.Redo();
        Check(!view.CanUndo && view.CanRedo, "Failed redo changed history");
        command.ThrowOnExecute = true;
        Throws<InvalidOperationException>(view.Redo);
        Check(!view.CanUndo && view.CanRedo, "Exception lost redo history");
    }

    static void TestExports()
    {
        var document = Document.Load(Path.Combine(root, "Documents", "Blumind Quick Help.bmd"));
        var map = (MindMap)document.Charts[0];
        Check(map.EnsureChartLayouted(), "Layout failed");
        using var image = map.CreateThumbImage(new Size(1200, 800));
        image.Save(Path.Combine(output, "preview.png"), ImageFormat.Png);
        var size = map.GetContentSize();
        var svg = new SvgDocument(size.Width, size.Height);
        var svgGraphics = new SvgGraphics(svg);
        new GeneralRender().Paint(map, new RenderArgs(svgGraphics, map, svgGraphics.Font(ChartBox.DefaultChartFont)));
        svg.Save(Path.Combine(output, "export.svg"));
        using var pdf = new PdfDocument();
        using (var graphics = XGraphics.FromPdfPage(pdf.AddPage()))
        {
            var pdfGraphics = new PdfGraphics(graphics);
            new GeneralRender().Paint(map, new RenderArgs(pdfGraphics, map, pdfGraphics.Font(ChartBox.DefaultChartFont)));
        }
        pdf.Save(Path.Combine(output, "export.pdf"));
        using var loaded = PdfSharp.Pdf.IO.PdfReader.Open(Path.Combine(output, "export.pdf"));
        Check(loaded.PageCount == 1 && XmlIO.Load(Path.Combine(output, "export.svg")).DocumentElement.LocalName == "svg", "Invalid exports");
    }

    static void Run(string name, Action test)
    {
        try { test(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception e) { failures++; Console.WriteLine("FAIL " + name + ": " + e); }
    }
    static async Task RunAsync(string name, Func<Task> test)
    {
        try { await test(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception e) { failures++; Console.WriteLine("FAIL " + name + ": " + e); }
    }
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Throws<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new Exception("Expected " + typeof(T).Name);
    }
    sealed class ProbeCommand : Command
    {
        public bool ExecuteResult = true, RollbackResult = true, ThrowOnExecute;
        public override string Name => "Probe";
        public override bool Execute() => ThrowOnExecute ? throw new InvalidOperationException("Simulated failure") : ExecuteResult;
        public override bool Rollback() => RollbackResult;
    }
}
