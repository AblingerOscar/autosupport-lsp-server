using autosupport_lsp_server.LSP;
using autosupport_lsp_server.Parsing;
using autosupport_lsp_server.Parsing.Impl;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace autosupport_lsp_server
{
    public class Document
    {
        private Document(Uri uri, IParser parser)
        {
            Uri = uri;
            this.parser = parser;
        }

        internal Uri Uri { get; }
        internal IList<string> Text { get; private set; } = new List<string>();
        public IParseResult? ParseResult { get; private set; }

        private readonly IParser parser;

        internal void ApplyChange(TextDocumentContentChangeEvent change)
        {
            if (change.Range == null)
            {
                // When no range is given, then the change includes the entire text of the file 
                Text = ConvertTextToList(change.Text);
            }
            else
            {
                // Else the range specifies the part that should be replaced
                ApplyPartialChange(change);
            }

            Reparse();
        }

        private void Reparse()
        {
            ParseResult = parser.Parse(Uri, Text.ToArray());
        }

        internal Identifier[] GetIdentifiersAtPosition(Position pos)
        {
            if (ParseResult == null)
                return new Identifier[0];

            return ParseResult.Identifiers
                .Where(iden => iden.References.Any(reference => pos.IsIn(reference.Range)))
                .ToArray();
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
            for (int i = (int)end.Line; i > (int)start.Line; --i)
            {
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
            }
            else
            {
                Text.Insert((int)pos.Line + 1, text[text.Count - 1] + Text[(int)pos.Line].Substring((int)pos.Character));
                Text[(int)pos.Line] = Text[(int)pos.Line].Substring(0, (int)pos.Character) + text[0];

                for (int i = text.Count - 2; i > 0; ++i)
                {
                    Text.Insert((int)pos.Line + 1, text[i]);
                }
            }
        }

        internal static Document CreateEmptyDocument(Uri uri, IParser parser)
        {
            return new Document(uri, parser);
        }

        internal static Document FromText(Uri uri, string text, IParser parser)
        {
            var doc = new Document(uri, parser)
            {
                Text = ConvertTextToList(text)
            };
            doc.Reparse();
            return doc;
        }

        private static IList<string> ConvertTextToList(string text)
        {
            return new List<string>(text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None));
        }
    }
}
