using autosupport_lsp_server;
using autosupport_lsp_server.Parsing.Impl;
using autosupport_lsp_server.Symbols;
using Moq;
using System.Collections.Generic;
using Tests.Terminals.Impl.Mocks;
using Xunit;

namespace Tests.Parsing.Impl
{
    public class ParserTest
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        void When_RuleWithOneTerminal_ThenParsesCorrectly(bool shouldParse)
        {
            // given
            string parseString = "terminalString";

            var terminal = new Mock<MockTerminal>
            {
                CallBase = true // for both Match functions & IsTerminal
            };
            terminal.SetupGet(t => t.MinimumNumberOfCharactersToParse).Returns(parseString.Length);

            terminal.Setup(t => t.TryParse(It.IsAny<string>())).Returns(shouldParse);

            var rule = new Mock<IRule>();
            rule.SetupGet(r => r.Symbols).Returns(new List<ISymbol>() { terminal.Object });

            var languageDefinition = new Mock<IAutosupportLanguageDefinition>();
            languageDefinition.SetupGet(el => el.StartRules).Returns(new string[] { "S" });
            languageDefinition.SetupGet(el => el.Rules).Returns(new Dictionary<string, IRule>()
            {
                { "S", rule.Object }
            });

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

            var nonTerminal = new Mock<MockNonTerminal>
            {
                CallBase = true // for both Match functions & IsTerminal
            };
            nonTerminal.SetupGet(nt => nt.ReferencedRule).Returns(referencedRuleName);

            var terminal = new Mock<MockTerminal>
            {
                CallBase = true // for both Match functions & IsTerminal
            };
            terminal.SetupGet(t => t.MinimumNumberOfCharactersToParse).Returns(parseString.Length);

            terminal.Setup(t => t.TryParse(It.IsAny<string>())).Returns(true);

            var baseRule = new Mock<IRule>();
            baseRule.SetupGet(r => r.Symbols).Returns(new List<ISymbol>() { nonTerminal.Object });

            var referencedRule = new Mock<IRule>();
            referencedRule.SetupGet(r => r.Symbols).Returns(new List<ISymbol>() { terminal.Object });


            var languageDefinition = new Mock<IAutosupportLanguageDefinition>();
            languageDefinition.SetupGet(el => el.StartRules).Returns(new string[] { "S" });
            languageDefinition.SetupGet(el => el.Rules).Returns(new Dictionary<string, IRule>()
            {
                { "S", baseRule.Object },
                { referencedRuleName, referencedRule.Object }
            });

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
