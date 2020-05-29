using autosupport_lsp_server;
using autosupport_lsp_server.Parsing.Impl;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using Xunit;

namespace Tests
{
    public class DocumentTest : DocumentBaseTest
    {
        const string uri = "uri";
        IList<string> text = new List<string>()
        {
            "First line",
            "Second line",
            "",  // empty line
            "Last line"
        };

        [Fact]
        public void UriIsAssignedProperly()
        {
            Document d1 = Document.CreateEmptyDocument(uri, NoOpParser());
            Assert.Equal(d1.Uri, uri);

            Document d2 = Document.FromText(uri, "some text", NoOpParser());
            Assert.Equal(d2.Uri, uri);
        }

        [Theory]
        [InlineData("\n")]
        [InlineData("\r\n")]
        [InlineData("\r")]
        public void TextIsAssignedProperly(string newline)
        {
            Document d = Document.FromText(uri, string.Join(newline, text), NoOpParser());
            Assert.Equal(text, d.Text);
        }

        [Fact]
        public void ChangeWithNoRange()
        {
            // given
            var newText = new List<string>()
            {
                "first line",
                "second line"
            };

            var change = new TextDocumentContentChangeEvent()
            {
                Range = null,
                Text = string.Join('\n', newText)
            };

            Document d = Document.FromText(uri, string.Join('\n', text), NoOpParser());

            // when
            d.ApplyChange(change);

            // then
            Assert.Equal(newText, d.Text);
        }

        [Theory]
        [InlineData(0, 0, 1, 0, new[] { "new linelast line" })]
        [InlineData(0, 0, 1, 4, new[] { "new line line" })]
        [InlineData(1, 4, 1, 5, new[] { "first line", "lastnew lineline" })]
        [InlineData(1, 2, 1, 2, new[] { "first line", "lanew linest line" })]
        [InlineData(1, 9, 1, 9, new[] { "first line", "last linenew line" })]
        public void ChangeWithSinglelineText(long startL, long startC, long endL, long endC, string[] result)
        {
            // given
            var originalText = new List<string>()
            {
                "first line",
                "last line"
               //012345678
            };
            var newText = "new line";
            var resultText = new List<string>(result);

            var change = new TextDocumentContentChangeEvent()
            {
                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(new Position(startL, startC), new Position(endL, endC)),
                Text = newText
            };

            Document d = Document.FromText(uri, string.Join('\n', originalText), NoOpParser());

            // when
            d.ApplyChange(change);

            // then
            Assert.Equal(resultText, d.Text);
        }

        [Theory]
        [InlineData(0, 0, 1, 0, new[] { "new line 1", "new line 2last line" })]
        [InlineData(0, 0, 1, 4, new[] { "new line 1", "new line 2 line" })]
        [InlineData(1, 4, 1, 5, new[] { "first line", "lastnew line 1", "new line 2line" })]
        [InlineData(1, 9, 1, 9, new[] { "first line", "last linenew line 1", "new line 2" })]
        public void ChangeWithMultilineText(long startL, long startC, long endL, long endC, string[] result)
        {
            // given
            var originalText = new List<string>()
            {
                "first line",
                "last line"
            };
            var newText = new List<string>()
            {
                "new line 1",
                "new line 2"
            };
            var resultText = new List<string>(result);

            var change = new TextDocumentContentChangeEvent()
            {
                Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(new Position(startL, startC), new Position(endL, endC)),
                Text = string.Join('\n', newText)
            };

            Document d = Document.FromText(uri, string.Join('\n', originalText), NoOpParser());

            // when
            d.ApplyChange(change);

            // then
            Assert.Equal(resultText, d.Text);
        }
    }
}
