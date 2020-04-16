using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;

namespace autosupport_lsp_server
{
    internal class Document
    {
        private Document(string uri)
        {
            Uri = uri;
        }

        internal string Uri { get; }
        internal string[] Text { get; private set; } = new string[0];

        internal void ApplyChange(TextDocumentContentChangeEvent change)
        {
            throw new NotImplementedException();
        }

        internal static Document CreateEmptyDocument(string uri)
        {
            return new Document(uri);
        }

        internal static Document FromText(string uri, string text)
        {
            return new Document(uri)
            {
                Text = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
            };
        }
    }
}
