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

            RemoveTextInRange(start, end);
            InsertText(start, newText);
        }

        private void RemoveTextInRange(Position start, Position end)
        {
            // Merge first and last line
            string restStrOnEndLine = Text[(int)end.Line].Substring((int)end.Character);
            Text[(int)start.Line] = Text[(int)start.Line].Substring(0, (int)start.Character) + restStrOnEndLine;

            // Remove all lines in between and the last line
            for (int i = (int)end.Line; i > (int)start.Line; --i) {
                Text.RemoveAt(i);
            }
        }

        private void InsertText(Position pos, IList<string> text)
        {
            if (text.Count == 0)
            {
                return;
            }

            string restStrOnEndLine = Text[(int)pos.Line].Substring((int)pos.Character);

            if (text.Count == 1)
            {
                Text[(int)pos.Line] = Text[(int)pos.Line].Substring(0, (int)pos.Character) + text[0] + restStrOnEndLine;
            } else
            {
                Text.Insert((int)pos.Line + 1, text[text.Count - 1] + Text[(int)pos.Line].Substring((int)pos.Character));
                Text[(int)pos.Line] = Text[(int)pos.Line].Substring(0, (int)pos.Character) + text[0];

                for(int i = text.Count - 2; i > 0; ++i)
                {
                    Text.Insert((int)pos.Line + 1, text[i]);
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
