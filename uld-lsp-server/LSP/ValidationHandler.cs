using uld.definition;
using uld.server.Parsing;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ILanguageServer = OmniSharp.Extensions.LanguageServer.Server.ILanguageServer;

namespace uld.server.LSP
{
    internal class ValidationHandler
    {
        private readonly ILanguageServer languageServer;
        private readonly IDocumentStore documentStore;

        public ValidationHandler(ILanguageServer languageServer, IDocumentStore documentStore)
        {
            this.languageServer = languageServer;
            this.documentStore = documentStore;
        }

        public void RunValidation(Uri uri, CancellationToken cancellationToken)
        {
            if (documentStore.Documents.TryGetValue(uri.ToString(), out var document))
            {
                var documentSpecificDiagnostics = GetDocumentSpecificErrors(uri, document);

                var allDocuments = Identifier.MergeIdentifiers(
                        documentStore.Documents.Values.Select(doc => doc.ParseResult?.Identifiers).WhereNotNull().ToArray());
                var declarationErrors = GetDeclarationErrorsOfIdentifiersForUri(allDocuments);

                languageServer.Document.PublishDiagnostics(
                    new PublishDiagnosticsParams()
                    {
                        Uri = uri,
                        Diagnostics = documentSpecificDiagnostics
                            .Union(declarationErrors.Select(Error2Diagnostic))
                            .ToArray()
                    });
            }
        }

        private IEnumerable<Diagnostic> GetDocumentSpecificErrors(Uri uri, Document document)
        {
            var errors = document.ParseResult?.Errors;

            if (errors == null)
                return Enumerable.Empty<Diagnostic>();

            return errors.Select(Error2Diagnostic);
        }

        private IEnumerable<Error> GetDeclarationErrorsOfIdentifiersForUri(Identifier[] identifiers)
        {
            return identifiers.SelectMany(identifier =>
            {
                if (identifier.Declaration == null)
                {
                    return identifier.References.Select(reference =>
                        new Error(reference.Uri, reference.Range, DiagnosticSeverity.Error, $"{identifier.Name} is used, but never declared"));
                }
                else if (!identifier.AllowsUseBeforeDeclared)
                {
                    return identifier.References
                        .Where(reference => reference.Range.Start.IsBefore(identifier.Declaration.Range.Start))
                        .Select(reference =>
                            new Error(
                                reference.Uri,
                                reference.Range,
                                DiagnosticSeverity.Error,
                                $"{identifier.Name} is used before it is declared",
                                new Error.ConnectedError(identifier.Declaration.Uri, identifier.Declaration.Range, "Declaration")));
                }
                else
                    return Enumerable.Empty<Error>();
            });
        }

        private static Diagnostic Error2Diagnostic(Error error)
        {
            return new Diagnostic()
            {
                Range = error.Range,
                Severity = error.Severity,
                Message = error.Reason,
                RelatedInformation = error.ConnectedErrors
                    .Select(ce => new DiagnosticRelatedInformation()
                    {
                        Location = new Location()
                        {
                            Uri = ce.Uri,
                            Range = ce.Range
                        },
                        Message = ce.Reason
                    })
                    .ToArray()
            };
        }
    }
}
