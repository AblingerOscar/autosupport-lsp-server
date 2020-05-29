using autosupport_lsp_server;
using autosupport_lsp_server.Parsing;
using autosupport_lsp_server.Parsing.Impl;
using autosupport_lsp_server.Symbols;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tests
{
    public abstract class DocumentBaseTest
    {
        protected IParser NoOpParser()
        {
            var parser = new Mock<IParser>();

            parser.Setup(p => p.Parse(It.IsAny<string[]>())).Returns(ParseResult().Object);

            return parser.Object;
        }

        protected Mock<IParseResult> ParseResult()
        {
            var result = new Mock<IParseResult>();

            return result;
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
    }
}
