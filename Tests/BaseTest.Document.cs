using autosupport_lsp_server;
using autosupport_lsp_server.Parsing;
using autosupport_lsp_server.Parsing.Impl;
using autosupport_lsp_server.Symbols;
using Moq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Tests
{
    public abstract partial class BaseTest
    {
        protected IParser NoOpParser()
        {
            var parser = new Mock<IParser>();

            parser.Setup(p => p.Parse(It.IsAny<string[]>())).Returns(ParseResult().Object);

            return parser.Object;
        }

        protected Mock<IAutosupportLanguageDefinition> LanguageDefinition(
                IRule rule,
                string? languageId = null,
                string? languageFilePattern = null
            )
        {
            return LanguageDefinition(
                languageId,
                languageFilePattern,
                new string[] { rule.Name },
                new Dictionary<string, IRule>()
                {
                    { rule.Name, rule }
                });
        }

        protected Mock<IAutosupportLanguageDefinition> LanguageDefinition(
                string? languageId = null,
                string? languageFilePattern = null,
                string[]? startRules = null,
                IDictionary<string, IRule>? rules = null
            )
        {
            var langDef = new Mock<IAutosupportLanguageDefinition>();

            if (languageId != null)
            {
                langDef.SetupGet(ld => ld.LanguageId).Returns(languageId);
            }

            if (languageFilePattern != null)
            {
                langDef.SetupGet(ld => ld.LanguageFilePattern).Returns(languageFilePattern);
            }

            if (startRules != null)
            {
                langDef.SetupGet(ld => ld.StartRules).Returns(startRules);
            }

            if (rules != null)
            {
                langDef.SetupGet(ld => ld.Rules).Returns(rules);
            }

            return langDef;
        }

        protected Mock<IDocumentStore> DocumentStore(
            IDictionary<string, Document>? documents = null,
            IAutosupportLanguageDefinition? languageDefinition = null,
            IParser? defaultParser = null
            )
        {
            var documentStore = new Mock<IDocumentStore>();

            if (documents != null)
                documentStore.SetupGet(ds => ds.Documents).Returns(documents);

            if (languageDefinition != null)
                documentStore.SetupGet(ds => ds.LanguageDefinition).Returns(languageDefinition);

            if (defaultParser != null)
                documentStore.Setup(ds => ds.CreateDefaultParser()).Returns(defaultParser);

            return documentStore;
        }

        protected Mock<IDocumentStore> DocumentStore(
            string documentUri, string text,
            IEnumerable<ISymbol> onlyRuleSymbols,
            IParseResult parseResult
            )
        {
            var parser = Parser(parseResult).Object;

            return DocumentStore(
                new Dictionary<string, Document>()
                {
                    { documentUri, Document.FromText(documentUri, text, parser) }
                },
                LanguageDefinition(Rule("S", onlyRuleSymbols.ToArray()).Object).Object,
                parser
                );
        }

        protected Mock<IDocumentStore> DocumentStore(
            string documentUri, string text,
            IAutosupportLanguageDefinition? languageDefinition = null,
            IParser? defaultParser = null
            )
        {
            var docParser = defaultParser ?? new Mock<IParser>().Object;

            return DocumentStore(
                new Dictionary<string, Document>()
                {
                    { documentUri, Document.FromText(documentUri, text, docParser) }
                },
                languageDefinition,
                defaultParser
            );
        }

        protected Mock<IParser> Parser(
                IParseResult? parseResult = null
            )
        {
            var parser = new Mock<IParser>();

            if (parseResult != null)
                parser.Setup(p => p.Parse(It.IsAny<string[]>())).Returns(parseResult);

            return parser;
        }

        protected Mock<IParseResult> ParseResult(
                IError[]? errors = null,
                string[]? possibleContinuations = null
            )
        {
            var parseResult = new Mock<IParseResult>();

            if (possibleContinuations != null)
                parseResult.SetupGet(pr => pr.PossibleContinuations).Returns(possibleContinuations);

            if (errors != null)
                parseResult.SetupGet(pr => pr.Errors).Returns(errors);

            return parseResult;
        }

        protected CompletionParams CompletionParams(string uri, string text)
        {
            return new CompletionParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = new Uri(uri) },
                Position = new Position(0, text.Length)
            };
        }
    }
}
