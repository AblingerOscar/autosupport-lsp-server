using uld.server.LSP;
using uld.server.Parsing;
using uld.server.Parsing.Impl;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace uld.server
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

        internal void UpdateText(string text)
        {
            Text = LSPUtils.ConvertTextToList(text);
            Reparse();
        }

        internal void ApplyChange(TextDocumentContentChangeEvent change)
        {
            if (change.Range == null)
            {
                // When no range is given, then the change includes the entire text of the file 
                Text = LSPUtils.ConvertTextToList(change.Text);
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
            var newText = LSPUtils.ConvertTextToList(change.Text);

            LSPUtils.ReplaceInText(Text, start, end, newText);
        }

        internal static Document CreateEmptyDocument(Uri uri, IParser parser)
        {
            return new Document(uri, parser);
        }

        internal static Document FromText(Uri uri, string text, IParser parser)
        {
            var doc = new Document(uri, parser)
            {
                Text = LSPUtils.ConvertTextToList(text)
            };
            doc.Reparse();
            return doc;
        }
    }
}
