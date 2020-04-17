using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;

namespace autosupport_lsp_server
{
    internal class Document
    {
        private Document(string uri)
        {
            Uri = uri;
        }

        internal string Uri { get; }
        internal IList<string> Text { get; private set; } = new List<string>();

        internal void ApplyChange(TextDocumentContentChangeEvent change)
        {
            if (change.Range == null)
            {
                // When no range is given, then the change includes the entire text of the file 
                Text = ConvertTextToList(change.Text);
            } else
            {
                // Else the range specifies the part that should be replaced
                ApplyPartialChange(change);
            }
        }

        private void ApplyPartialChange(TextDocumentContentChangeEvent change)
        {
            var start = change.Range.Start;
            var end = change.Range.End;
            var newText = ConvertTextToList(change.Text);

            if (start.Line == end.Line)
            {
                int line = (int)start.Line;
                Text[line] = Text[line].Substring(0, (int)start.Character + 1)
                    + newText[0]
                    + Text[(int)end.Line].Substring((int)start.Character);
            }
            else
            {
                Text[(int)start.Line] = Text[(int)start.Character].Substring(0, (int)start.Line + 1) + newText[0];
                Text[(int)end.Line] = newText[newText.Count - 1] + Text[(int)end.Line].Substring((int)start.Character);

                for (int i = (int)start.Line + 1; i < (int)end.Line; ++i) {
                    Text.RemoveAt(i);
                }
                for (int i = newText.Count - 2; i > 0; --i)
                {
                    Text.Insert((int)start.Line + 1, Text[i]);
                }
            }
        }

        internal static Document CreateEmptyDocument(string uri)
        {
            return new Document(uri);
        }

        internal static Document FromText(string uri, string text)
        {
            return new Document(uri)
            {
                Text = ConvertTextToList(text)
            };
        }

        private static IList<string> ConvertTextToList(string text)
        {
            return new List<string>(text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None));
        }
    }
}
