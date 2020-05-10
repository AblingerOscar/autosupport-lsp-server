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
            rule.SetupGet(r => r.MinimumNumberOfCharactersToParse).Returns(1);
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
    }
}
