using autosupport_lsp_server.Symbols.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Xunit;

namespace Tests.Terminals.Impl
{
    public class NonTerminalTest
    {
        [Fact]
        public void DeserializationWorksCorrectly()
        {
            // given a Terminal xElement
            string nonTerminalXml = Helpers.ReadFile(@"Terminals.Impl.Files.CorrectNonTerminal.xml");
            var element = XElement.Parse(nonTerminalXml);

            // when
            var nonTerminal = NonTerminal.FromXLinq(element, null);

            // then
            Assert.Equal("id", nonTerminal.Id);
            Assert.False(nonTerminal.IsTerminal);
            Assert.Equal("file:///c:/user/groot/defs/nonTerminal.atg", nonTerminal.Source.AbsoluteUri);
            Assert.Equal("This is just a NonTerminal documentation", nonTerminal.Documentation);
        }
    }
}
