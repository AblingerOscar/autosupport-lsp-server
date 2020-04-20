using System.Xml.Linq;

namespace autosupport_lsp_server.SyntaxTree
{
    interface ISyntaxTreeNode
    {
        XElement SerializeToXLinq();

        static ISyntaxTreeNode FromXLinq(XElement element)
        {
            return null;
        }
    }
}