using autosupport_lsp_server.Terminals.Impl;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using Xunit;

namespace Tests.Terminals.Impl
{
    public class TerminalTest
    {
        [Fact]
        public void DeserializationWorksCorrectly()
        {
            // given a Terminal xElement
            string nonTerminalXml = Helpers.ReadFile(@"Terminals.Impl.Files.CorrectTerminal.xml");
            var element = XElement.Parse(nonTerminalXml);

            // when
            var terminal = Terminal.FromXLinq(element, null);

            // then
            Assert.Equal("id", terminal.Id);
            Assert.True(terminal.IsTerminal);
            Assert.Equal("file:///c:/user/groot/defs/terminal.atg", terminal.Source.AbsoluteUri);
            Assert.Equal("This is just a Terminal documentation", terminal.Documentation);
        }
    }
}
