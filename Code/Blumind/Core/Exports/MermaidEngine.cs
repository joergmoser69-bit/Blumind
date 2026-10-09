using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Blumind.Globalization;
using Blumind.Model.Documents;
using Blumind.Model.MindMaps;
using Blumind.Model.Widgets;

namespace Blumind.Core.Exports
{
    // A portable hierarchy export. The BMD document remains the editable source.
    class MermaidEngine : ChartsExportEngine
    {
        readonly bool markdown;
        const string Limitations = "Hierarchy export: layout, styling, icons/images, progress widgets and cross-links are not included.";

        public MermaidEngine(bool markdown = false) { this.markdown = markdown; }

        public override string TypeMime => markdown ? DocumentType.MermaidMarkdown.TypeMime : DocumentType.Mermaid.TypeMime;
        protected override bool SupportMultiCharts => markdown;

        protected override bool ExportChartToFile(Document document, ChartPage chart, string filename)
        {
            return ExportChartsToFile(document, new[] { chart }, filename);
        }

        protected override bool ExportChartsToFile(Document document, IEnumerable<ChartPage> charts, string filename)
        {
            var pages = charts.ToArray();
            if (pages.Length == 0 || pages.Any(page => page is not MindMap)) return false;
            WriteFile(pages.Cast<MindMap>(), filename, markdown);
            return true;
        }

        internal static void WriteFile(IEnumerable<MindMap> maps, string filename, bool markdown)
        {
            string content = Serialize(maps, markdown);
            AtomicFile.Write(filename, output => output.Write(new UTF8Encoding(false).GetBytes(content)));
        }

        internal static string Serialize(IEnumerable<MindMap> maps, bool markdown)
        {
            var pages = maps.ToArray();
            if (pages.Length == 0 || (!markdown && pages.Length != 1))
                throw new ArgumentException("A Mermaid file contains one map; Markdown can contain multiple maps.", nameof(maps));
            var result = new StringBuilder();
            foreach (var map in pages)
            {
                if (map.Root == null) throw new ArgumentException("The map has no root.", nameof(maps));
                var topics = EnumerateTopics(map.Root).ToArray();
                if (markdown)
                    result.Append("# ").Append(EscapeMarkdown((map.Name ?? map.Root.Text ?? "Mindmap").Replace('\r', ' ').Replace('\n', ' '))).Append("\n\n```mermaid\n");
                result.Append("mindmap\n");
                for (int index = 0; index < topics.Length; index++)
                {
                    var item = topics[index];
                    result.Append(' ', (item.Depth + 1) * 2).Append('n').Append(index.ToString(CultureInfo.InvariantCulture));
                    result.Append(item.Depth == 0 ? "(\"" : "[\"").Append(EscapeLabel(item.Topic.Text));
                    result.Append(item.Depth == 0 ? "\")\n" : "\"]\n");
                }
                if (markdown)
                {
                    result.Append("```\n\n").Append(EscapeMarkdown(Lang._(Limitations))).Append("\n\n");
                    AppendNote(result, Lang._("Map notes"), map.Remark);
                    for (int index = 0; index < topics.Length; index++)
                    {
                        var topic = topics[index].Topic;
                        string heading = "n" + index.ToString(CultureInfo.InvariantCulture) + " — " + (topic.Text ?? string.Empty);
                        AppendNote(result, heading, topic.Remark);
                        int noteIndex = 0;
                        foreach (var note in topic.Widgets.OfType<NoteWidget>())
                            AppendNote(result, heading + " — " + Lang._("Notes") + " " + (++noteIndex).ToString(CultureInfo.InvariantCulture), note.Remark);
                    }
                }
                else
                {
                    result.Append("%% ").Append(Limitations).Append("\n");
                    result.Append("%% Notes are included in the Mermaid Markdown export only.\n");
                }
            }
            return result.ToString();
        }

        static IEnumerable<(Topic Topic, int Depth)> EnumerateTopics(Topic root)
        {
            var pending = new Stack<(Topic Topic, int Depth)>();
            pending.Push((root, 0));
            while (pending.Count > 0)
            {
                var item = pending.Pop();
                yield return item;
                for (int index = item.Topic.Children.Count - 1; index >= 0; index--)
                    pending.Push((item.Topic.Children[index], item.Depth + 1));
            }
        }

        static string EscapeLabel(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return " ";
            var label = new StringBuilder();
            foreach (char character in value.Replace("\r\n", "\n").Replace('\r', '\n'))
            {
                if (character == '\n') label.Append("<br/>");
                else if (character == '\t') label.Append(' ');
                // Mermaid processes %% directives before its quoted-label parser.
                else if ("\"#&<>`*_\\%{}$".IndexOf(character) >= 0)
                    label.Append('#').Append(((int)character).ToString(CultureInfo.InvariantCulture)).Append(';');
                else if (!char.IsControl(character)) label.Append(character);
            }
            return label.Length == 0 ? " " : label.ToString();
        }

        static void AppendNote(StringBuilder result, string heading, string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return;
            // Keep readable paragraph boundaries; this deliberately exports plain text.
            string text = Regex.Replace(html, @"<(script|style)\b[^>]*>.*?</\1\s*>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            text = Regex.Replace(text, @"<br\s*/?>|</?(?:p|div|h[1-6]|li|tr|blockquote|pre|ul|ol|table)\b[^>]*>", "\n", RegexOptions.IgnoreCase);
            text = ST.HtmlToText(text).Replace("\r\n", "\n").Replace('\r', '\n');
            text = Regex.Replace(text, @"\n[ \t]*\n(?:[ \t]*\n)+", "\n\n");
            if (string.IsNullOrWhiteSpace(text)) return;
            result.Append("## ").Append(EscapeMarkdown(heading.Replace('\r', ' ').Replace('\n', ' '))).Append("\n\n");
            foreach (string line in text.Split('\n'))
            {
                if (line.Length > 0) result.Append(EscapeMarkdown(line)).Append("  ");
                result.Append('\n');
            }
            result.Append('\n');
        }

        static string EscapeMarkdown(string value)
        {
            var result = new StringBuilder();
            foreach (char character in value)
            {
                if ("\\`*_{}[]()#+-.!|>".IndexOf(character) >= 0) result.Append('\\');
                if (character == '<') result.Append("&lt;");
                else if (character == '&') result.Append("&amp;");
                else result.Append(character);
            }
            return result.ToString();
        }
    }
}
