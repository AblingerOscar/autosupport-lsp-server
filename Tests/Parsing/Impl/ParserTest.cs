using autosupport_lsp_server.Parsing.Impl;
using autosupport_lsp_server.Symbols;
using autosupport_lsp_server.Symbols.Impl.Terminals;
using Moq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Tests.Parsing.Impl
{
    public class ParserTest : BaseTest
    {
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

            var languageDefinition = AutosupportLanguageDefinition(
                startRule: "S",
                rules: new KeyValuePair<string, IRule>("S", rule.Object)
                );

            // when
            var result = new Parser(languageDefinition.Object)
                .Parse(new string[] { parseString });

            // then
            Assert.True(result.Finished);
        }

        [Theory]
        [InlineData("", "aterminal")]
        [InlineData("a", "terminal")]
        void When_MoreInputIsExpectedFromTerminal_ThenHavePossibleContinuations(string firstLine, string expectedContinuation)
        {
            // given
            var terminal = Terminal(
                    "aterminal",
                    false
                );

            var rule = Rule(
                    symbols: terminal.Object
                );

            var languageDefinition = AutosupportLanguageDefinition(
                    startRule: "S",
                    rules: new Dictionary<string, IRule>() { { "S", rule.Object } }
                );

            // when
            var result = new Parser(languageDefinition.Object)
                .Parse(new string[] { firstLine });

            // then
            terminal.Verify(t => t.TryParse(It.IsAny<string>()), Times.Never());
            Assert.NotEmpty(result.PossibleContinuations);

            var firstChoiceContinuation = result.PossibleContinuations[0];
            Assert.Equal(expectedContinuation, firstChoiceContinuation.TextEdit?.NewText ?? firstChoiceContinuation.Label);
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
                .Parse(new string[] { parseString });

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
                .Parse(new string[] { " " });

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
                    symbols: new ISymbol[] { oneOf.Object, successfulTerminal.Object }
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
                .Parse(new string[] { "12" });

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
            var parseResult = new Parser(langDef.Object).Parse(new string[] { text });

            // then
            Assert.NotNull(parseResult.PossibleContinuations);

            var labels = parseResult.PossibleContinuations.Select(ci => ci.Label);

            foreach (var kw in keywords)
                Assert.Single(parseResult.PossibleContinuations, ci => ci.Label == kw && ci.Kind == CompletionItemKind.Keyword);
        }

    }
}
