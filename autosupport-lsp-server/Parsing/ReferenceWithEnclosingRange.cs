using autosupport_lsp_server.LSP;
using System;
using System.Collections.Generic;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace autosupport_lsp_server.Parsing
{
    public readonly struct ReferenceWithEnclosingRange : IReferenceWithEnclosingRange
    {
        public ReferenceWithEnclosingRange(Uri uri, Range range, Range? enclosingDeclarationRange)
        {
            Uri = uri;
            Range = range;
            EnclosingRange = enclosingDeclarationRange;
        }

        public ReferenceWithEnclosingRange(IReferenceWithEnclosingRange other)
        {
            Uri = other.Uri;
            Range = other.Range.Clone();

            if (other.EnclosingRange != null)
                EnclosingRange = new Range(
                    other.EnclosingRange.Start.Clone(),
                    other.EnclosingRange.End.Clone()
                    );
            else
                EnclosingRange = null;
        }

        public Uri Uri { get; }
        public Range Range { get; }
        public Range? EnclosingRange { get; }

        public override bool Equals(object? obj)
        {
            return obj is ReferenceWithEnclosingRange reference &&
                   base.Equals(obj) &&
                   EqualityComparer<Range>.Default.Equals(EnclosingRange, reference.EnclosingRange);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), EnclosingRange);
        }
    }
}
