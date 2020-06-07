using autosupport_lsp_server;
using autosupport_lsp_server.LSP;
using autosupport_lsp_server.Parsing.Impl;
using autosupport_lsp_server.Serialization;
using autosupport_lsp_server.Symbols.Impl.Terminals;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Xunit;

namespace Tests.LSP
{
    /// <summary>
    /// Mostly integration tests from the point of the AutocompletionHandler
    /// </summary>
    public class AutocompletionHandlerTest : BaseTest
    {
        public static readonly string[] VarAndPrintKeywords = new string[]
        {
            "Program", "var", "=", ";", "print"
        };

        public static readonly IAutosupportLanguageDefinition VarAndPrintLanguageDefintion =
            autosupport_lsp_server.AutosupportLanguageDefinition.FromXLinq(XElement.Parse(Helpers.ReadFile("Files.VarAndPrint.def")), InterfaceDeserializer.Instance);
        
        [Fact]
        public async void When_Handling_NeverReturnDuplicateEntries()
        {
            // given
            var uri = "file:///test.txt";
            var text = "Program a var var = 0; "; // should return keywords & 'var' as a variable name
            var docStore = DocumentStore(uri, text, VarAndPrintLanguageDefintion, new Parser(VarAndPrintLanguageDefintion));

            var aucoHandler = new AutocompletionHandler(docStore.Object);

            // when
            var result = await aucoHandler.Handle(CompletionParams(uri, text), new CancellationToken());

            // then: return every entry only once
            Assert.NotNull(result);

            var resultSet = result
                .Select(r => (r.TextEdit?.NewText ?? r.Label, r.Kind))
                .ToHashSet();

            Assert.Equal(resultSet.Count(), result.Count());
        }

        [Theory]
        [InlineData("", new string[] { "Program" })] // start of program -> correct kw must be first in line
        [InlineData("Pro", new string[] { "gram" })] // start of keyword -> rest of keyword should be suggested
        [InlineData("Program ", new string[0])] // next any identifier -> no special autocompletion
        [InlineData("Program a var b = 0;", new string[] { "var", "b" })] // defined an identifier -> identifier should be suggested as continuation, but not the program name (type check)
        [InlineData("Program a var b = 00; b", new string[] { "print" })] // only one possible kw next -> first in line
        [InlineData("Program a var bar = 0; b", new string[] { "ar" })] // start of identifier -> rest of identifier should be suggested
        public async void When_HandlingContinuableText_ReturnAtLeastAllContinuationsInTheCorrectOrder(string text, string[] expectedContinuations)
        {
            // given
            var uri = "file:///test.txt";
            var docStore = DocumentStore(uri, text, VarAndPrintLanguageDefintion, new Parser(VarAndPrintLanguageDefintion));

            var aucoHandler = new AutocompletionHandler(docStore.Object);

            // when
            var result = await aucoHandler.Handle(CompletionParams(uri, text), new CancellationToken());

            // then: expectedContinuations are always the first options
            Assert.NotNull(result);

            var resultAsList = result.Items.ToList();
            for(int i = 0; i < expectedContinuations.Length; ++i)
            {
                // Label may not be the same as the continuation e.g. completion may be 'ar', but label 'var',
                //      because 'v' is already present in the file -> contains not equal
                Assert.Contains(expectedContinuations[i], resultAsList.ElementAt(i).Label);

                // if they're not the same, however, TextEdit has to exist and contain the exact Continuation
                if (expectedContinuations[i] != resultAsList.ElementAt(i).Label)
                {
                    Assert.Equal(expectedContinuations[i], resultAsList.ElementAt(i).TextEdit?.NewText);
                }
            }
        }

        [Fact]
        public async void When_HandlingContinuableTextWithIdentifierNamedLikeKeyword_ReturnIdentifierAndKeywordWithDifferentKinds()
        {
            // given
            var uri = "file:///test.txt";
            var text = "Program a var var = 0; ";
            var docStore = DocumentStore(uri, text, VarAndPrintLanguageDefintion, new Parser(VarAndPrintLanguageDefintion));

            var aucoHandler = new AutocompletionHandler(docStore.Object);

            // when
            var result = await aucoHandler.Handle(CompletionParams(uri, text), new CancellationToken());

            // then: the result contains 'var' twice, once as a variable and once as a keyword
            Assert.NotNull(result);
            Assert.Equal(VarAndPrintKeywords.Length + 1, result.Count());

            Assert.Contains(("var", CompletionItemKind.Variable), result.Select(r => (r.TextEdit?.NewText ?? r.Label, r.Kind)));
        }
    }
}

