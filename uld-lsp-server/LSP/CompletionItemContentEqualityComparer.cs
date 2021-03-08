using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace uld.server.LSP
{
    /// <summary>
    /// Compares CompletionItems based on their label and Kind ignoring TextEdits and other Properties
    /// </summary>
    public class CompletionItemContentEqualityComparer : IEqualityComparer<CompletionItem>
    {
        public bool Equals([AllowNull] CompletionItem x, [AllowNull] CompletionItem y)
        {
            if (x == null)
                return y == null;

            if (y == null)
                return false;

            return x.Label == y.Label && x.Kind == y.Kind;
        }

        public int GetHashCode([DisallowNull] CompletionItem ci)
        {
            return $"{ci.Kind}{ci.Label.GetHashCode()}".GetHashCode();
        }
    }
}
