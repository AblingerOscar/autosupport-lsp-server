using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Diagnostics.CodeAnalysis;

namespace autosupport_lsp_server.Parsing.Impl
{
    public class CompletionItemPrioritizationItem: IComparable<CompletionItemPrioritizationItem>
    {
        public CompletionItemPrioritizationItem(ContinuationType continuationSource, CompletionItem completionItem, bool? isExpectedType = null)
        {
            ContinuationSource = continuationSource;
            CompletionItem = completionItem;
            IsExpectedType = isExpectedType;
        }

        public CompletionItem CompletionItem { get; }
        public ContinuationType ContinuationSource { get; set; }

        public bool? IsExpectedType { get; }

        public int CompareTo([AllowNull] CompletionItemPrioritizationItem other)
        {
            if (other == null)
                return -1;

            if (ContinuationSource != other.ContinuationSource
                || (ContinuationSource != ContinuationType.CompletionOfIdentifier
                    && ContinuationSource != ContinuationType.NextIdentifier))
                return other.ContinuationSource - ContinuationSource;

            if (IsExpectedType == other.IsExpectedType)
                return 0;

            // if this is of the expected type, but not other
            if (IsExpectedType.HasValue && IsExpectedType.Value)
                return -1;

            // if other is of the expected type, but not this
            if (other.IsExpectedType.HasValue && other.IsExpectedType.Value)
                return 1;

            // else: only possible combination left is null & false
            //    -> the one with false has precedence over null
            return IsExpectedType.HasValue ? -1 : 1;
        }

        public override string? ToString()
        {
            string type = IsExpectedType.HasValue
                ? (IsExpectedType.Value ? "<correct type>" : "<incorrect type>")
                : "<no type>";

            string editText = CompletionItem.TextEdit == null
                ? ""
                : $"({CompletionItem.TextEdit.NewText})";

            return $"{ContinuationSource}: {type} {CompletionItem.Label}{editText}";
        }
    }

    public enum ContinuationType
    {
        CompletionOfIdentifier = 5,
        CompletionOfKeyword = 4,
        NextIdentifier = 3,
        NextKeyword = 2,
        Identifier = 1,
        Keyword = 0
    }
}
