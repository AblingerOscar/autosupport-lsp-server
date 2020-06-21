using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace autosupport_lsp_server.Parsing
{
    public interface IReferenceWithEnclosingRange : IReference
    {
        /// <summary>
        /// Includes not only the identifier itself, but also enclosing relevant information like
        /// comment, documentation, parameters etc.
        /// This information is typically used to highlight the range in the editor.
        /// </summary>
        Range? EnclosingRange { get; }
    }
}