using autosupport_lsp_server;
using autosupport_lsp_server.Shared;
using autosupport_lsp_server.Symbols;
using autosupport_lsp_server.Symbols.Impl;
using autosupport_lsp_server.Symbols.Impl.Terminals;
using Moq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Tests.Mocks.Symbols;

namespace Tests
{
    public abstract partial class BaseTest
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

        protected ITerminal StringTerminal(string content, bool? shouldParse = null)
        {
            var mock = new Mock<MockStringTerminal>(content, shouldParse)
            {
                CallBase = true
            };

            return mock.Object;
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
                IEnumerable<ISymbol>? symbols = null
            )
        {
            return Rule(
                    name,
                    symbols?.Select(s => new Either<string, ISymbol>(s)).ToArray() ?? new Either<string, ISymbol>[0]
                );
        }

        protected Mock<IRule> Rule(
                string? name = null,
                params Either<string, ISymbol>[] symbols
            )
        {
            var mock = new Mock<IRule>();

            if (name != null)
                mock.SetupGet(r => r.Name).Returns(name);

            if (symbols.Length > 0)
            {
                mock.SetupGet(r => r.Symbols).Returns(symbols.Select(either =>
                    either.Match(
                        str => new Action(str),
                        sym => sym
                        )).ToImmutableList());
            }

            return mock;
        }

        protected Mock<IAutosupportLanguageDefinition> AutosupportLanguageDefinition(
                string[]? startRules = null,
                IDictionary<string, IRule>? rules = null,
                CommentRules? commentRules = null
            )
        {
            var mock = new Mock<IAutosupportLanguageDefinition>();

            if (startRules != null)
                mock.SetupGet(el => el.StartRules).Returns(startRules);

            if (rules != null)
                mock.SetupGet(el => el.Rules).Returns(rules);

            if (commentRules.HasValue)
                mock.SetupGet(el => el.CommentRules).Returns(commentRules.Value);
            else
                mock.SetupGet(el => el.CommentRules).Returns(new CommentRules(new CommentRule[0], new CommentRule[0]));

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
