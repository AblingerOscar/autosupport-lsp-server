using autosupport_lsp_server;
using autosupport_lsp_server.Symbols;
using Moq;
using System.Collections.Generic;
using System.Collections.Immutable;
using Tests.Mocks.Symbols;

namespace Tests
{
    public abstract class SymbolsBaseTest
    {
        protected Mock<MockTerminal> Terminal(string content, bool? shouldParse)
        {
            return Terminal(
                    minimumNumberOfCharactersToParse: content.Length,
                    shouldParse,
                    possibleContent: new string[] { content }
                );
        }

        protected Mock<MockTerminal> Terminal(
                int? minimumNumberOfCharactersToParse = null,
                bool? shouldParse = null,
                string[]? possibleContent = null
            )
        {
            var mock = new Mock<MockTerminal>
            {
                CallBase = true // for both Match functions
            };

            if (minimumNumberOfCharactersToParse.HasValue)
            {
                mock.SetupGet(t => t.MinimumNumberOfCharactersToParse).Returns(minimumNumberOfCharactersToParse.Value);
            }

            if (shouldParse.HasValue)
            {
                mock.Setup(t => t.TryParse(It.IsAny<string>())).Returns(shouldParse.Value);
            }

            if (possibleContent != null)
            {
                mock.SetupGet(t => t.PossibleContent).Returns(possibleContent);
            }

            return mock;
        }

        protected Mock<MockNonTerminal> NonTerminal(
                string? referencedRuleName = null
            )
        {
            var mock = new Mock<MockNonTerminal>
            {
                CallBase = true // for both Match functions
            };

            if (referencedRuleName != null)
            {
                mock.SetupGet(nt => nt.ReferencedRule).Returns(referencedRuleName);
            }

            return mock;
        }

        protected Mock<MockOneOf> OneOf(
                bool? allowNone = null,
                params string[] options
            )
        {
            var mock = new Mock<MockOneOf>()
            {
                CallBase = true // for both Match functions
            };

            if (allowNone.HasValue)
            {
                mock.SetupGet(oo => oo.AllowNone).Returns(allowNone.Value);
            }

            if (options.Length != 0)
            {
                mock.SetupGet(oo => oo.Options).Returns(options);
            }

            return mock;
        }

        protected Mock<IRule> Rule(
                string? name = null,
                params ISymbol[] symbols
            )
        {
            var mock = new Mock<IRule>();

            if (name != null)
            {
                mock.SetupGet(r => r.Name).Returns(name);
            }

            if (symbols.Length > 0)
            {
                mock.SetupGet(r => r.Symbols).Returns(new List<ISymbol>(symbols).ToImmutableList());
            }

            return mock;
        }

        protected Mock<IAutosupportLanguageDefinition> AutosupportLanguageDefinition(
                string[]? startRules = null,
                IDictionary<string, IRule>? rules = null
            )
        {
            var mock = new Mock<IAutosupportLanguageDefinition>();

            if (startRules != null)
            {
                mock.SetupGet(el => el.StartRules).Returns(startRules);
            }

            if (rules != null)
            {
                mock.SetupGet(el => el.Rules).Returns(rules);
            }

            return mock;
        }

        protected Mock<IAutosupportLanguageDefinition> AutosupportLanguageDefinition(
                string? startRule = null,
                IDictionary<string, IRule>? rules = null
            )
        {
            return AutosupportLanguageDefinition(
                    startRule == null ? null : new string[1] { startRule },
                    rules
                );
        }

        protected Mock<IAutosupportLanguageDefinition> AutosupportLanguageDefinition(
                string[]? startRules = null,
                params KeyValuePair<string, IRule>[] rules
            )
        {
            return AutosupportLanguageDefinition(
                    startRules,
                    rules == null ? null : new Dictionary<string, IRule>(rules)
                );
        }

        protected Mock<IAutosupportLanguageDefinition> AutosupportLanguageDefinition(
                string? startRule = null,
                params KeyValuePair<string, IRule>[] rules
            )
        {
            return AutosupportLanguageDefinition(
                    startRule == null ? null : new string[1] { startRule },
                    rules == null ? null : new Dictionary<string, IRule>(rules)
                );
        }
    }
}
