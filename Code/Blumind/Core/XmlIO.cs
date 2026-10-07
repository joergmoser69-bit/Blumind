using System;
using System.IO;
using System.Xml;

namespace Blumind.Core
{
    // All file, import and clipboard XML follows the same limits. No external resources.
    static class XmlIO
    {
        internal const long MaxDocumentCharacters = 64 * 1024 * 1024;
        internal const int MaxDepth = 256;

        static XmlReaderSettings Settings => new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = MaxDocumentCharacters,
            MaxCharactersFromEntities = 1024,
            CloseInput = false
        };

        public static XmlDocument Load(string filename)
        {
            using var input = File.OpenRead(filename);
            return Load(input);
        }

        public static XmlDocument Load(Stream input)
        {
            using var reader = XmlReader.Create(input, Settings);
            return Load(reader);
        }

        public static XmlDocument Parse(string xml)
        {
            using var input = new StringReader(xml);
            using var reader = XmlReader.Create(input, Settings);
            return Load(reader);
        }

        static XmlDocument Load(XmlReader reader)
        {
            var document = new XmlDocument { XmlResolver = null };
            document.Load(reader);
            ValidateDepth(document);
            return document;
        }

        public static void ValidateDepth(XmlDocument document)
        {
            XmlNode node = document;
            int depth = 0;
            while (node != null)
            {
                if (depth > MaxDepth)
                    throw new XmlException("The document contains too many nested elements.");
                if (node.FirstChild != null)
                {
                    node = node.FirstChild;
                    depth++;
                }
                else
                {
                    while (node != null && node.NextSibling == null)
                    {
                        node = node.ParentNode;
                        depth--;
                    }
                    if (node != null)
                        node = node.NextSibling;
                }
            }
        }
    }
}
