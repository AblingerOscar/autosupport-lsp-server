using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace autosupport_lsp_server.Parsing
{
    public class DeclarationReference : Reference
    {
        public DeclarationReference(Uri uri, Range range, Range? enclosingDeclarationRange) : base(uri, range)
        {
            EnclosingDeclarationRange = enclosingDeclarationRange;
        }

        /// <summary>
        /// Includes not only the identifier itself, but also enclosing relevant information like
        /// comment, documentation, parameters etc.
        /// This information is typically used to highlight the range in the editor.
        /// </summary>
        public Range? EnclosingDeclarationRange { get; }

        public override bool Equals(object? obj)
        {
            return obj is DeclarationReference reference &&
                   base.Equals(obj) &&
                   EqualityComparer<Range>.Default.Equals(EnclosingDeclarationRange, reference.EnclosingDeclarationRange);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), EnclosingDeclarationRange);
        }
    }
}
