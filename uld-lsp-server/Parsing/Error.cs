using uld.server.LSP;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace uld.server.Parsing
{
    public readonly struct Error
    {
        public Uri Uri { get; }
        public Range Range { get; }
        public DiagnosticSeverity Severity { get; }
        public string Reason { get; }
        public ConnectedError[] ConnectedErrors { get; }

        public Error(Uri uri, Range range, DiagnosticSeverity severity, string reason, ConnectedError connectedError)
            : this(uri, range, severity, reason, new ConnectedError[] { connectedError }) { }

        public Error(Uri uri, Range range, DiagnosticSeverity severity, string reason, ConnectedError[]? connectedErrors = null)
        {
            Uri = uri;
            Range = range;
            Severity = severity;
            Reason = reason;
            ConnectedErrors = connectedErrors ?? new ConnectedError[0];
        }

        public override bool Equals(object? obj)
            => obj is Error error &&
                   EqualityComparer<Range>.Default.Equals(Range, error.Range) &&
                   Severity == error.Severity &&
                   Reason == error.Reason;

        public override int GetHashCode() => HashCode.Combine(Range, Severity, Reason);

        public override string? ToString()
            => $"{Severity} '{Reason}' in {Uri} at {Range.ToNiceString()} with {ConnectedErrors.Length} connected Errors";

        public readonly struct ConnectedError
        {
            public Uri Uri { get; }
            public Range Range { get; }
            public string Reason { get; }

            public ConnectedError(Uri uri, Range range, string reason)
                => (Uri, Range, Reason) = (uri, range, reason);

            public override bool Equals(object? obj)
                => obj is ConnectedError error &&
                       EqualityComparer<Uri>.Default.Equals(Uri, error.Uri) &&
                       EqualityComparer<Range>.Default.Equals(Range, error.Range) &&
                       Reason == error.Reason;

            public override int GetHashCode() => HashCode.Combine(Uri, Range, Reason);

            public override string? ToString()
                => $"'{Reason}' in {Uri} at {Range.ToNiceString()}";
        }
    }
}
