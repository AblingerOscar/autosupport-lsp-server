using autosupport_lsp_server.LSP;
using System;
using System.Collections.Generic;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace autosupport_lsp_server.Parsing
{
    public readonly struct Reference : IReference
    {
        public Reference(Uri uri, Range range)
        {
            Uri = uri;
            Range = range;
        }

        public Reference(IReference other)
        {
            Uri = other.Uri;
            Range = other.Range.Clone();
        }

        public Uri Uri { get; }
        public Range Range { get; }

        public override bool Equals(object? obj)
        {
            return obj is Reference reference
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
            return $"{Uri}: {Range.ToNiceString()}";
        }
    }
}
