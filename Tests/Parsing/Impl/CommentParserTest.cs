using autosupport_lsp_server;
using autosupport_lsp_server.Parsing.Impl;
using Xunit;

namespace Tests.Parsing.Impl
{
    public class CommentParserTest
    {
        private readonly static CommentRules rules =
            new CommentRules(
                    new[] { new CommentRule("/*", "*/", " "), new CommentRule("//", "\n", " ") },
                    new[] { new CommentRule("/**", "*/", " "), new CommentRule("///", "\n", " ") }
                );
        private readonly CommentParser parser = new CommentParser(rules);

        [Fact]
        public void PartOfLineAsComment()
        {
            // when
            var result = parser.GetNextComment("/* this is a comment */this is not a comment anymore");

            // then
            Assert.True(result.HasValue);
            Assert.Null(result!.Value.Documentation);
            Assert.Equal("/* this is a comment */".Length, result.Value.CommentLength);
            Assert.Equal(" ", result.Value.Replacement);
        }

        [Fact]
        public void EndOfLineCommentsAreRecognised()
        {
            // when
            var result = parser.GetNextComment("// this is full line comment\n" +
                                               "this is not a comment anymore");

            // then
            Assert.True(result.HasValue);
            Assert.Null(result!.Value.Documentation);
            Assert.Equal("// this is full line comment\n".Length, result.Value.CommentLength);
            Assert.Equal(" ", result.Value.Replacement);
        }

        [Fact]
        public void MultipleLineComment()
        {
            // when
            var result = parser.GetNextComment("/* this is a multi-\n" +
                                               "line comment */this is not a comment anymore");

            // then
            Assert.True(result.HasValue);
            Assert.Null(result!.Value.Documentation);
            Assert.Equal("/* this is a multi-\nline comment */".Length, result.Value.CommentLength);
            Assert.Equal(" ", result.Value.Replacement);
        }

        [Fact]
        public void NotAComment()
        {
            // when
            var result = parser.GetNextComment(" // this is full line comment\n" +
                                               "this is not a comment anymore");

            // then
            Assert.False(result.HasValue);
        }

        [Fact]
        public void DocumentationCommentsArePreferred()
        {
            // when
            var result = parser.GetNextComment("/// this is full line doc comment\n" +
                                               "this is not a comment anymore");

            // then
            Assert.True(result.HasValue);
            Assert.Equal(" this is full line doc comment", result!.Value.Documentation);
            Assert.Equal("/// this is full line doc comment\n".Length, result.Value.CommentLength);
            Assert.Equal(" ", result.Value.Replacement);
        }

        [Fact]
        public void MultiLineDocumentationComments()
        {
            // when
            var result = parser.GetNextComment("/** this is multi-\n" +
                                                "line doc comment */\n" +
                                               "this is not a comment anymore");

            // then
            Assert.True(result.HasValue);
            Assert.Equal(" this is multi-\nline doc comment ", result!.Value.Documentation);
            Assert.Equal("/** this is multi-\nline doc comment */".Length, result.Value.CommentLength);
            Assert.Equal(" ", result.Value.Replacement);
        }
    }
}
