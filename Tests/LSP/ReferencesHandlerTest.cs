using autosupport_lsp_server;
using autosupport_lsp_server.LSP;
using autosupport_lsp_server.Parsing.Impl;
using autosupport_lsp_server.Serialization;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Xunit;

namespace Tests.LSP
{
    public class ReferencesHandlerTest : BaseTest
    {
        public static readonly IAutosupportLanguageDefinition VarAndPrintLanguageDefintion =
            autosupport_lsp_server.AutosupportLanguageDefinition.FromXLinq(XElement.Parse(Helpers.ReadFile("Files.VarAndPrint.def")), InterfaceDeserializer.Instance);
        
        [Theory]
        [InlineData(@"Program a
var b = 0;
b print;

b print;", 1)]
        [InlineData(@"Program a
var var = 0;
var print;

var print;", 3)]
        public async void FindAllReferences(string text, int identifierLength)
        {
            // given
            var uri = "file:///test.txt";
            var docStore = DocumentStore(uri, text, VarAndPrintLanguageDefintion, new Parser(VarAndPrintLanguageDefintion));

            var refHandler = new ReferencesHandler(docStore.Object);

            var searchPosition = new Position(2, 0);
            var otherPositions = new List<Range>() {
                new Range(searchPosition, new Position(searchPosition.Line, searchPosition.Character + identifierLength)),
                new Range(new Position(1, 4), new Position(1, 4 + identifierLength)),
                new Range(new Position(4, 0), new Position(4, 0 + identifierLength))
            };

            // when
            var result = await refHandler.Handle(ReferenceParams(uri, searchPosition), new CancellationToken());

            // then: expectedContinuations are always the first options
            Assert.NotNull(result);

            foreach (var item in result)
            {
                Assert.Equal(uri, item.Uri.ToString());

                var positionIdx = otherPositions.IndexOf(item.Range);

                Assert.NotEqual(-1, positionIdx);

                otherPositions.RemoveAt(positionIdx);
            }

            Assert.Empty(otherPositions);
        }
    }
}
