using autosupport_lsp_server.LSP;
using System;
using System.Collections.Generic;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace autosupport_lsp_server.Parsing
{
    public class Reference
    {
        public Reference(Uri uri, Range range)
        {
            Uri = uri;
            Range = range;
        }

        public Reference(DeclarationReference other)
        {
            Uri = other.Uri;
            Range = new Range(other.Range.Start.Clone(), other.Range.End.Clone());
        }

        public Uri Uri { get; }
        public Range Range { get; }

        public override bool Equals(object? obj)
        {
            return obj is DeclarationReference reference
                   && base.Equals(obj)
                   && EqualityComparer<Uri>.Default.Equals(Uri, reference.Uri)
                   && EqualityComparer<Range>.Default.Equals(Range, reference.Range);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Uri, Range);
        }

        public override string? ToString()
        {
            return $"{Uri}: ({Range.Start.Line},{Range.Start.Character})-({Range.End.Line},{Range.End.Character})";
        }
    }
}
