using autosupport_lsp_server;
using autosupport_lsp_server.LSP;
using autosupport_lsp_server.Parsing.Impl;
using autosupport_lsp_server.Serialization;
using System;
using System.Xml.Linq;
using System.Linq;
using Xunit;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using CancellationToken = System.Threading.CancellationToken;
using ImplementationParams = OmniSharp.Extensions.LanguageServer.Protocol.Models.ImplementationParams;
using TextDocumentIdentifier = OmniSharp.Extensions.LanguageServer.Protocol.Models.TextDocumentIdentifier;
using Position = OmniSharp.Extensions.LanguageServer.Protocol.Models.Position;
using ImplementationCapability = OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities.ImplementationCapability;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using System.Collections.Generic;

namespace Tests.LSP
{
    public class ImplementationHandlerTest : BaseTest
    {
        public static readonly IAutosupportLanguageDefinition VarAndPrintLanguageDefintion =
            autosupport_lsp_server.AutosupportLanguageDefinition.FromXLinq(XElement.Parse(Helpers.ReadFile("Files.VarAndPrint.def")), InterfaceDeserializer.Instance);

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async void FindImplementationInSameDocument(bool linkSupport)
        {
            // given
            var text = @"Program prog
var variable = 0;
variable print;
variable print;
";
            var uri = new Uri("file:///program");
            var parser = new Parser(VarAndPrintLanguageDefintion);
            var docStore = DocumentStore(uri.ToString(), text, VarAndPrintLanguageDefintion, parser);

            var handler = new ImplementationHandler(docStore.Object);

            handler.SetCapability(new ImplementationCapability()
            {
                LinkSupport = linkSupport
            });

            var expected = new LocationLink()
            {
                OriginSelectionRange = new Range(new Position(1, 4), new Position(1, 12)),
                TargetUri = uri,
                //TargetRange = new Range(new Position(1, 4), new Position(1, 11)),
                TargetSelectionRange = new Range(new Position(1, 4), new Position(1, 12))
            };

            // when
            var result = await handler.Handle(new ImplementationParams()
            {
                TextDocument = new TextDocumentIdentifier(uri),
                Position = new Position(1, 6),
            }, new CancellationToken());

            // then

            var resultAsList = result.ToList();

            Assert.Single(resultAsList);

            Assert.Equal(linkSupport, resultAsList[0].IsLocationLink);

            AssertLocationOrLocationLink(linkSupport, expected, resultAsList[0]);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async void FindImplementationAfterUsage(bool linkSupport)
        {
            // given
            var text = @"Program prog
variable print;
var variable = 0;
variable print;
";
            var uri = new Uri("file:///program");
            var parser = new Parser(VarAndPrintLanguageDefintion);
            var docStore = DocumentStore(uri.ToString(), text, VarAndPrintLanguageDefintion, parser);

            var handler = new ImplementationHandler(docStore.Object);

            handler.SetCapability(new ImplementationCapability()
            {
                LinkSupport = linkSupport
            });

            var expected = new LocationLink()
            {
                OriginSelectionRange = new Range(new Position(1, 0), new Position(1, 8)),
                TargetUri = uri,
                //TargetRange = new Range(new Position(1, 4), new Position(1, 11)),
                TargetSelectionRange = new Range(new Position(2, 4), new Position(2, 12))
            };

            // when
            var result = await handler.Handle(new ImplementationParams()
            {
                TextDocument = new TextDocumentIdentifier(uri),
                Position = new Position(1, 6),
            }, new CancellationToken());

            // then

            var resultAsList = result.ToList();

            Assert.Single(resultAsList);

            Assert.Equal(linkSupport, resultAsList[0].IsLocationLink);

            AssertLocationOrLocationLink(linkSupport, expected, resultAsList[0]);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async void FindImplementationInDifferentDocument(bool linkSupport)
        {
            // given
            var text1 = @"Program prog1
variable print;";
            var text2 = @"Program prog2
var variable = 0;";
            var document1Uri = new Uri("file:///program1");
            var document2Uri = new Uri("file:///program2");
            var parser = new Parser(VarAndPrintLanguageDefintion);
            var docStore = DocumentStore(
                new Dictionary<string, Document>()
                {
                    { document1Uri.ToString(), Document.FromText(document1Uri, text1, parser) },
                    { document2Uri.ToString(), Document.FromText(document2Uri, text2, parser) }
                },
                VarAndPrintLanguageDefintion,
                parser);

            var handler = new ImplementationHandler(docStore.Object);

            handler.SetCapability(new ImplementationCapability()
            {
                LinkSupport = linkSupport
            });

            var expected = new LocationLink()
            {
                OriginSelectionRange = new Range(new Position(1, 0), new Position(1, 8)),
                TargetUri = document2Uri,
                //TargetRange = new Range(new Position(1, 4), new Position(1, 11)),
                TargetSelectionRange = new Range(new Position(1, 4), new Position(1, 12))
            };

            // when
            var result = await handler.Handle(new ImplementationParams()
            {
                TextDocument = new TextDocumentIdentifier(document1Uri),
                Position = new Position(1, 2),
            }, new CancellationToken());

            // then
            var resultAsList = result.ToList();

            Assert.Single(resultAsList);

            Assert.Equal(linkSupport, resultAsList[0].IsLocationLink);

            AssertLocationOrLocationLink(linkSupport, expected, resultAsList[0]);
        }

        private static void AssertLocationOrLocationLink(bool linkSupport, LocationLink expected, LocationOrLocationLink actual)
        {
            if (linkSupport)
            {
                var locationLink = actual.LocationLink;

                Assert.Equal(expected.OriginSelectionRange, locationLink.OriginSelectionRange);
                //Assert.Equal(expected.TargetRange, locationLink.TargetRange);
                Assert.Equal(expected.TargetSelectionRange, locationLink.TargetSelectionRange);
                Assert.Equal(expected.TargetUri, locationLink.TargetUri);
            }
            else
            {
                var location = actual.Location;

                Assert.Equal(expected.TargetUri, location.Uri);
                Assert.Equal(expected.TargetSelectionRange, location.Range);
            }
        }
    }
}
