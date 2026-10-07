using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Blumind.Configuration;
using Blumind.Core;
using Blumind.Dialogs;
using Blumind.Globalization;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Blumind.Controls
{
    public partial class HtmlEditBox : Control
    {
        readonly WebView2 browser;
        readonly TaskCompletionSource ready = new TaskCompletionSource();
        Task initialization;
        string html = string.Empty;
        string plainText = string.Empty;
        bool readOnly;
        int revision;
        TextBox fallback;

        public HtmlEditBox()
        {
            browser = new WebView2 { Dock = DockStyle.Fill, AllowExternalDrop = false };
            Controls.Add(browser);
            BackColor = SystemColors.Window;
        }

        internal static string UserDataFolder { get; set; }
        internal WebView2 Browser => browser;
        protected bool BrowserReady { get; private set; }
        protected string OriginalText { get; private set; } = string.Empty;
        protected override Size DefaultSize => new Size(100, 100);

        [Category("Appearance"), Bindable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override string Text
        {
            get => html;
            set
            {
                html = OriginalText = value ?? string.Empty;
                plainText = ST.HtmlToText(html);
                revision++;
                if (fallback != null)
                    fallback.Text = html;
                else if (BrowserReady)
                    RunScript(UpdateContentScript());
            }
        }

        [Browsable(false)]
        public string PlainText => plainText;

        [DefaultValue(false)]
        public bool ReadOnly
        {
            get => readOnly;
            set
            {
                readOnly = value;
                if (fallback != null) fallback.ReadOnly = value;
                if (BrowserReady)
                    RunScript("window.editor.readOnly(" + JsonSerializer.Serialize(value) + ")");
            }
        }

        protected override async void OnCreateControl()
        {
            base.OnCreateControl();
            if (!this.IsDesignMode()) await EnsureReadyAsync();
        }

        internal Task EnsureReadyAsync() => initialization ??= InitializeAsync();

        async Task InitializeAsync()
        {
            try
            {
                string folder = UserDataFolder ?? Path.Combine(ProgramEnvironment.ApplicationDataDirectory, "WebView2");
                var environment = await CoreWebView2Environment.CreateAsync(null, folder);
                if (IsDisposed) return;
                await browser.EnsureCoreWebView2Async(environment);
                if (IsDisposed) return;
                var core = browser.CoreWebView2;
                core.Settings.AreDefaultContextMenusEnabled = true;
                core.Settings.AreDevToolsEnabled = false;
                core.Settings.AreHostObjectsAllowed = false;
                core.Settings.IsStatusBarEnabled = false;
                core.Settings.IsWebMessageEnabled = true;
                core.PermissionRequested += (_, e) => e.State = CoreWebView2PermissionState.Deny;
                core.NewWindowRequested += (_, e) => e.Handled = true;
                // NavigateToString may expose a data: URI during its initial navigation.
                // After the trusted local page has loaded, all document navigation is blocked.
                core.NavigationStarting += (_, e) => e.Cancel = BrowserReady;
                core.WebMessageReceived += OnWebMessage;
                core.NavigationCompleted += (_, e) =>
                {
                    if (!e.IsSuccess) ready.TrySetException(new InvalidOperationException("The note editor could not be loaded: " + e.WebErrorStatus));
                };
                using var input = typeof(HtmlEditBox).Assembly.GetManifestResourceStream("Blumind.Resources.html_editor.html");
                using var reader = new StreamReader(input);
                core.NavigateToString(reader.ReadToEnd());
                await ready.Task.WaitAsync(TimeSpan.FromSeconds(20));
                if (IsDisposed) return;
                BrowserReady = true;
                await core.ExecuteScriptAsync(UpdateContentScript());
                await core.ExecuteScriptAsync(StyleScript());
            }
            catch (Exception exception)
            {
                if (IsDisposed) return;
                BrowserReady = false;
                Helper.WriteLog(exception);
                browser.Visible = false;
                // Preserve notes and allow source editing when the Edge runtime is unavailable.
                fallback = new TextBox { Dock = DockStyle.Fill, Multiline = true,
                    ScrollBars = ScrollBars.Both, Text = html, ReadOnly = readOnly };
                fallback.TextChanged += (_, _) =>
                {
                    if (html == fallback.Text) return;
                    html = fallback.Text;
                    plainText = ST.HtmlToText(html);
                    OnTextChanged(EventArgs.Empty);
                };
                var information = new Label { Dock = DockStyle.Top, AutoSize = true,
                    Text = Lang._("WebView2 unavailable – HTML source editor") };
                Controls.Add(fallback);
                Controls.Add(information);
                fallback.BringToFront();
                information.BringToFront();
            }
        }

        string UpdateContentScript() => "window.editor.set(" + JsonSerializer.Serialize(html) + "," +
            revision + "," + JsonSerializer.Serialize(readOnly) + ")";

        string StyleScript() => "window.editor.font(" + JsonSerializer.Serialize(Font.FontFamily.Name) + "," +
            JsonSerializer.Serialize(Font.SizeInPoints.ToString(System.Globalization.CultureInfo.InvariantCulture) + "pt") + ")";

        void OnWebMessage(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            if (e.Source != "about:blank") return;
            using var message = JsonDocument.Parse(e.WebMessageAsJson);
            var state = message.RootElement;
            if (state.TryGetProperty("ready", out var isReady) && isReady.GetBoolean())
            {
                ready.TrySetResult();
                return;
            }
            if (state.GetProperty("revision").GetInt32() != revision) return;
            if (state.TryGetProperty("link", out var link))
            {
                if (IsAllowedLink(link.GetString())) Helper.OpenUrl(link.GetString());
                return;
            }
            ApplyState(state);
        }

        void ApplyState(JsonElement state)
        {
            plainText = state.GetProperty("text").GetString();
            if (state.GetProperty("changed").GetBoolean() && !ReadOnly)
            {
                string edited = state.GetProperty("html").GetString();
                if (edited != html)
                {
                    html = edited;
                    OnTextChanged(EventArgs.Empty);
                }
            }
        }

        internal static bool IsAllowedLink(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            (uri.Scheme == "https" || uri.Scheme == "http" || uri.Scheme == "mailto");

        async void RunScript(string script)
        {
            try
            {
                if (BrowserReady && !IsDisposed) await browser.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch (Exception e) when (e is InvalidOperationException || e is ObjectDisposedException ||
                e is System.Runtime.InteropServices.COMException)
            {
                if (!IsDisposed) Helper.WriteLog(e);
            }
        }

        public async Task EndEditAsync()
        {
            await EnsureReadyAsync();
            if (BrowserReady && !IsDisposed)
            {
                int expectedRevision = revision;
                string result = await browser.CoreWebView2.ExecuteScriptAsync("window.editor.state()");
                using var state = JsonDocument.Parse(result);
                if (revision == expectedRevision) ApplyState(state.RootElement);
            }
            EndEdit();
        }

        public bool EndEdit()
        {
            if (ReadOnly || OriginalText == html) return false;
            OriginalText = html;
            OnTextChanged(EventArgs.Empty);
            return true;
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            if (BrowserReady) RunScript(StyleScript());
        }

        public bool ExecCommand(string command, bool showUI, object value)
        {
            if (!BrowserReady || (ReadOnly && command != HtmlCommandIdentifiers.Copy)) return false;
            if (command == HtmlCommandIdentifiers.CreateLink)
            {
                using var dialog = new InputDialog(command == HtmlCommandIdentifiers.CreateLink ? "Link" : "Image", "URL");
                if (dialog.ShowDialog(this) != DialogResult.OK || !IsAllowedLink(dialog.Value)) return false;
                value = dialog.Value;
            }
            if (command == HtmlCommandIdentifiers.InsertImage)
            {
                using var dialog = new OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg;*.gif;*.bmp" };
                if (dialog.ShowDialog(this) != DialogResult.OK) return false;
                using var image = Image.FromFile(dialog.FileName);
                using var data = new MemoryStream();
                image.Save(data, System.Drawing.Imaging.ImageFormat.Png);
                value = "data:image/png;base64," + Convert.ToBase64String(data.ToArray());
            }
            if (command == HtmlCommandIdentifiers.Paste)
            {
                string pasted = Clipboard.ContainsText(TextDataFormat.Html)
                    ? ClipboardHelper.GetHtml() ?? string.Empty
                    : System.Net.WebUtility.HtmlEncode(Clipboard.GetText()).Replace("\n", "<br>");
                RunScript("window.editor.paste(" + JsonSerializer.Serialize(pasted) + ")");
            }
            else
                RunScript("window.editor.command(" + JsonSerializer.Serialize(command) + "," + JsonSerializer.Serialize(value) + ")");
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ready.TrySetCanceled();
            base.Dispose(disposing);
        }
    }
}
