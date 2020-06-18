using autosupport_lsp_server.Parsing.Impl;
using autosupport_lsp_server.Symbols;
using autosupport_lsp_server.Symbols.Impl.Terminals;
using Moq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Sprache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;
using Position = OmniSharp.Extensions.LanguageServer.Protocol.Models.Position;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c547cac37abd99c8db225ef2f6c8a3602f3b3606cc9891605d02baa56104f4cfc0734aa39b93bf7852f7d9266654753cc297e7d2edfe0bac1cdcf9f717241550e0a7b191195b7667bb4f64bcb8e2121380fd1d9d46ad2d92d2d15605093924cceaf74c4861eff62abf69b9291ed0a340e113be11e6a7d3113e92484cf7045cc7")]
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

            var languageDefinition = AutosupportLanguageDefinition(
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
                    symbols: terminal.Object
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

        private Mock<MockStringTerminal> StringTerminal(string content, bool? shouldParse = null)
        {
            var mock = new Mock<MockStringTerminal>(content, shouldParse)
            {
                CallBase = true
            };

            return mock;
        }


        internal class MockStringTerminal : StringTerminal
        {
            private bool? shouldParse;

            public MockStringTerminal(string str, bool? shouldParse) : base(str)
            {
                this.shouldParse = shouldParse;
            }

            protected override Parser<string> Parser {
                get {
                    if (shouldParse.HasValue)
                        return shouldParse.Value
                            ? Parser.Return(String)
                            : (Parser<string>)Parser.Return(String).Not();
                    return base.Parser;
                }
            }
        }
    }
}
