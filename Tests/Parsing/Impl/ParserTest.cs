using autosupport_lsp_server;
using autosupport_lsp_server.Parsing;
using autosupport_lsp_server.Parsing.Impl;
using autosupport_lsp_server.Symbols;
using Moq;
using Sprache;
using System.Collections.Generic;
using Tests.Terminals.Impl.Mocks;
using Xunit;

namespace Tests.Parsing.Impl
{
    public class ParserTest : SymbolsBaseTest
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        void When_RuleWithOneTerminal_ThenParsesCorrectly(bool shouldParse)
        {
            // given
            string parseString = "terminalString";

            var terminal = Terminal(
                minimumNumberOfCharactersToParse: parseString.Length,
                shouldParse: shouldParse
                );

            var rule = Rule(
                symbols: terminal.Object
                );

            var languageDefinition = AutosupportLanguageDefinition(
                startRule: "S",
                rules: new KeyValuePair<string, IRule>("S", rule.Object)
                );

            // when
            var result = Parser.Parse(
                languageDefinition.Object,
                Document.FromText("uri", "stringToken"));

            // then
            Assert.Equal(shouldParse, result.FinishedSuccessfully);
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
            var result = Parser.Parse(
                languageDefinition.Object,
                Document.FromText("uri", "stringToken"));

            // then
            Assert.True(result.FinishedSuccessfully);
            terminal.Verify(t => t.TryParse(It.IsAny<string>()), Times.Once());
        }
    }
}
