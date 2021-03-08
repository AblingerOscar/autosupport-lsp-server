using uld.server.LSP;
using uld.server.Parsing.Impl;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Xunit;
using uld.definition;
using uld.definition.Serialization;

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

        public static readonly ILanguageDefinition VarAndPrintLanguageDefintion =
            uld.definition.LanguageDefinition.FromXLinq(XElement.Parse(Helpers.ReadFile("Files.VarAndPrint.def")), InterfaceDeserializer.Instance);
        
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
        [InlineData("Program a var b = 0;", new string[] { "b", "var", "a" })]
                // defined two identifiers -> identifiers should be suggested as continuation; 'b' has precedence thanks to type
        [InlineData("Program a \nvar b = 0;", new string[] { "b", "var", "a" })] // same as previous one, but this time over two lines
        [InlineData("Program a \nvar b = 0;\n", new string[] { "b", "var", "a" })] // same as previous one, but this time over two lines (and trailing newline)
        [InlineData("Program a \nvar b = 00; \nb", new string[] { " print" })] // only one possible kw next -> first in line
        [InlineData("Program a \r\nvar bar = 0; \r\nb", new string[] { "ar" })] // start of identifier -> rest of identifier should be suggested
        [InlineData(@"Program a
var b = 0;
b print;

b print;", new string[] { "b", "var", "a" })]
        [InlineData(@"Program a
var foo = 0;
var bar = 15;", new string[] { "foo", "bar", "var", "a" })]
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
                Assert.Contains(expectedContinuations[i].Trim(), resultAsList.ElementAt(i).Label);

                // if they're not the same, however, TextEdit has to exist and while having the same text, has
                //      to have a correct range, which results in the correct continuation
                if (expectedContinuations[i] != resultAsList.ElementAt(i).Label)
                {
                    var textEdit = resultAsList.ElementAt(i).TextEdit;

                    Assert.NotNull(textEdit);
                    Assert.Contains(expectedContinuations[i], textEdit?.NewText);
                    Assert.Equal(textEdit!.Range.End.Line, textEdit.Range.End.Line);
                    Assert.Equal(expectedContinuations[i], textEdit?.NewText.Substring((int)(textEdit.Range.End.Character - textEdit.Range.Start.Character)));
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
            // two variables ('a' and 'var') where defined and should also be returned
            Assert.Equal(VarAndPrintKeywords.Length + 2, result.Count());

            Assert.Contains(("var", CompletionItemKind.Variable), result.Select(r => (r.TextEdit?.NewText ?? r.Label, r.Kind)));
        }
    }
}

