using autosupport_lsp_server.Parsing.Impl;
using autosupport_lsp_server.Shared;
using autosupport_lsp_server.Symbols;
using autosupport_lsp_server.Symbols.Impl.Terminals;
using Moq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Sprache;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Position = OmniSharp.Extensions.LanguageServer.Protocol.Models.Position;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Tests.Parsing.Impl
{
    public class ParserTest : BaseTest
    {
        private readonly Uri uri = new Uri("file:///");

        [Fact]
        void When_RuleWithOneTerminalThatParsesCorrectly_ThenFinish()
        {
            // given
            string parseString = "terminalString";

            var terminal = Terminal(
                minimumNumberOfCharactersToParse: parseString.Length,
                shouldParse: true
                );

            var rule = Rule(
                symbols: terminal.Object
                );

            var languageDefinition = LanguageDefinition(
                startRule: "S",
                rules: new KeyValuePair<string, IRule>("S", rule.Object)
                );

            // when
            var result = new Parser(languageDefinition.Object)
                .Parse(uri, new string[] { parseString });

            // then
            Assert.True(result.Finished);
        }

        [Theory]
        [InlineData("")]
        [InlineData("a")]
        void When_MoreInputIsExpectedFromTerminal_ThenHavePossibleContinuations(string firstLine)
        {
            // given
            var terminal = StringTerminal(
                    "aterminal",
                    false
                );

            var rule = Rule(
                    symbols: new Either<string, ISymbol>(terminal)
                );

            var languageDefinition = AutosupportLanguageDefinition(
                    startRule: "S",
                    rules: new Dictionary<string, IRule>() { { "S", rule.Object } }
                );

            // when
            var result = new Parser(languageDefinition.Object)
                .Parse(uri, new string[] { firstLine });

            // then
            Assert.NotEmpty(result.PossibleContinuations);

            var firstChoiceContinuation = result.PossibleContinuations[0];
            Assert.Equal("aterminal", firstChoiceContinuation.Label);

            if (firstChoiceContinuation.TextEdit != null)
            {
                Assert.Equal(new Position(0, 0), firstChoiceContinuation.TextEdit.Range.Start);
                Assert.Equal(new Position(0, firstLine.Length), firstChoiceContinuation.TextEdit.Range.End);
            }
        }

        [Fact]
        void When_RuleWithNonTerminal_ThenResolvesCorrectly()
        {
            // given
            string referencedRuleName = "ReferencedRule";
            string parseString = "terminalString";

            var nonTerminal = NonTerminal(referencedRuleName);

            var terminal = Terminal(
                    minimumNumberOfCharactersToParse: parseString.Length,
                    shouldParse: true
                );

            var baseRule = Rule(
                    symbols: nonTerminal.Object
                );
            var referencedRule = Rule(
                    name: referencedRuleName,
                    symbols: terminal.Object
                );

            var languageDefinition = AutosupportLanguageDefinition(
                    startRule: "S",
                    rules: new Dictionary<string, IRule>()
                    {
                        { "S", baseRule.Object },
                        { referencedRuleName, referencedRule.Object }
                    }
                );

            // when
            var result = new Parser(languageDefinition.Object)
                .Parse(uri, new string[] { parseString });

            // then
            Assert.True(result.Finished);
            terminal.Verify(t => t.TryParse(It.IsAny<string>()), Times.Once());
        }

        [Fact]
        void When_RuleWithOneOf_WithOneCorrect_ThenReturnsSuccessfully()
        {
            // given
            var terminalSucceedsName = "terminalSucceeds";
            var terminalFailsName = "terminalFails";

            var terminalSucceeds = Terminal(
                minimumNumberOfCharactersToParse: 1,
                shouldParse: true);

            var terminalFails = Terminal(
                minimumNumberOfCharactersToParse: 1,
                shouldParse: false);

            var oneOf = OneOf(
                false,
                terminalFailsName, terminalSucceedsName);

            var terminalSucceedsRule = Rule(symbols: terminalSucceeds.Object);
            var terminalFailsRule = Rule(symbols: terminalFails.Object);

            var rule = Rule(
                    name: "S",
                    symbols: oneOf.Object
                );

            var languageDefinition = AutosupportLanguageDefinition(
                    startRule: "S",
                    rules: new Dictionary<string, IRule>()
                    {
                        { "S", rule.Object },
                        { terminalFailsName, terminalFailsRule.Object },
                        { terminalSucceedsName, terminalSucceedsRule.Object }
                    }
                );

            // when
            var result = new Parser(languageDefinition.Object)
                .Parse(uri, new string[] { " " });

            // then
            Assert.True(result.Finished);
            terminalSucceeds.Verify(t => t.TryParse(It.IsAny<string>()), Times.Once());
            terminalFails.Verify(t => t.TryParse(It.IsAny<string>()), Times.Once());
        }

        [Fact]
        void When_RuleWithOneOf_WithAllowNone_ThenIgnoringItIsAllowed()
        {
            // given
            var terminalRuleName = "terminalRule";

            var successfulTerminal = Terminal(
                    minimumNumberOfCharactersToParse: 2,
                    shouldParse: true
                );

            var failingTerminal = Terminal(
                    minimumNumberOfCharactersToParse: 2,
                    shouldParse: false
                );

            var oneOf = OneOf(
                    allowNone: true,
                    options: terminalRuleName
                );

            var baseRule = Rule(
                    symbols: new Either<string, ISymbol>[] { oneOf.Object, successfulTerminal.Object }
                );
            var terminalRule = Rule(
                    symbols: failingTerminal.Object
                );

            var languageDefinition = AutosupportLanguageDefinition(
                    startRule: "S",
                    rules: new Dictionary<string, IRule>()
                    {
                        { "S", baseRule.Object },
                        { terminalRuleName, terminalRule.Object }
                    }
                );

            // when
            var result = new Parser(languageDefinition.Object)
                .Parse(uri, new string[] { "12" });

            // then
            Assert.True(result.Finished);
        }

        [Theory]
        [InlineData("")] // empty text
        [InlineData("InvalidText")] // some text
        public async void AlwaysAtLeastReturnTheKeywords(string text)
        {
            // given
            CompletionItem[] continuations = new CompletionItem[] {
                new CompletionItem() {
                    Label = "foo",
                    Kind = CompletionItemKind.Variable
                },
                new CompletionItem() {
                    Label = "bar",
                    Kind = CompletionItemKind.Variable
                },
            };

            string[] keywords = new string[]
            {
                "keyword1",
                "keyword2",
                "bar"
            };

            var langDef = LanguageDefinition(Rule("", keywords.Select(kw => new StringTerminal(kw)).ToArray()).Object);

            // when
            var parseResult = new Parser(langDef.Object).Parse(uri, new string[] { text });

            // then
            Assert.NotNull(parseResult.PossibleContinuations);

            var labels = parseResult.PossibleContinuations.Select(ci => ci.Label);

            foreach (var kw in keywords)
                Assert.Single(parseResult.PossibleContinuations, ci => ci.Label == kw && ci.Kind == CompletionItemKind.Keyword);
        }

        public static IEnumerable<object[]> data_IdentifiersHaveCorrectRangeEvenWithComments = new[]
        {
            new object[] { "var myIdentifier;", " ", new[] { "myIdentifier" }, new[] { new[] { 0L, 4L, 0L, 16L } } },
            new object[] { "var myIdentifier/*comment*/;", " ", new[] { "myIdentifier" }, new[] { new[] { 0L, 4L, 0L, 16L } } },
            new object[] { "var myIdentifier/*comment*/;", "", new[] { "myIdentifier" }, new[] { new[] { 0L, 4L, 0L, 16L } } },
            new object[] { "var /*comment*/myIdentifier;", " ", new[] { "myIdentifier" }, new[] { new[] { 0L, 15L, 0L, 27L } } },
            new object[] { "var /*comment*/myIdentifier;", "", new[] { "myIdentifier" }, new[] { new[] { 0L, 15L, 0L, 27L } } },
            new object[] { "var my/*comment*/Identifier;", "", new[] { "myIdentifier" }, new[] { new[] { 0L, 4L, 0L, 27L } } },
        };

        [Theory]
        [MemberData(nameof(data_IdentifiersHaveCorrectRangeEvenWithComments))]
        public void IdentifiersHaveCorrectRangeEvenWithComments(string text, string commentReplacement, string[] identifiers, long[][] identifierRanges)
        {
            // given
            var startRule = Rule("S",
                    new[] {
                        new Either<string, ISymbol>(StringTerminal("var")),
                        new Either<string, ISymbol>(OneOf(true, "Ws").Object),
                        IAction.IDENTIFIER,
                        new Either<string, ISymbol>(NonTerminal("Identifier").Object),
                        IAction.IDENTIFIER,
                        new Either<string, ISymbol>(OneOf(true, "Ws").Object),
                        new Either<string, ISymbol>(StringTerminal(";"))
                    }).Object;
            var optionalWSRule = Rule("Ws",
                    new[] {
                        new Either<string, ISymbol>(new AnyWhitespaceTerminal()),
                        new Either<string, ISymbol>(OneOf(true, "Ws").Object),
                    }).Object;
            var identifierRule = Rule("Identifier",
                    new[] {
                        new Either<string, ISymbol>(new AnyLetterTerminal()),
                        new Either<string, ISymbol>(OneOf(true, "Identifier").Object),
                    }).Object;

            var langDef = LanguageDefinition(
                null,
                null,
                new[] { startRule.Name },
                new Dictionary<string, IRule>()
                {
                    { startRule.Name, startRule },
                    { optionalWSRule.Name, optionalWSRule },
                    { identifierRule.Name, identifierRule }
                },
                CommentRules(("/*", "*/", commentReplacement)));

            var parser = new Parser(langDef.Object);
            var uri = new Uri("unused:///");

            // when
            var result = parser.Parse(uri, new[] { text });

            // then
            Assert.Equal(identifiers, result.Identifiers.Select(i => i.Name).ToArray());
            Assert.Equal(identifierRanges, result.Identifiers
                    .Select(i => i.References.First().Range)
                    .Select(r => new[] { r.Start.Line, r.Start.Character, r.End.Line, r.End.Character }));
        }
    }
}
