using autosupport_lsp_server.Serialization;
using OmniSharp.Extensions.LanguageServer.Server;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace autosupport_lsp_server
{
    class Program
    {
        static async Task Main(string[] args)
        {
            SetupDocumentStore(args);

            var server = await LanguageServer.From(options =>
            {
                options
                    .WithInput(Console.OpenStandardInput())
                    .WithOutput(Console.OpenStandardOutput())
                    .WithHandler<TextDocumentSyncHandler>();
            });

            await server.WaitForExit;
        }

        static private void SetupDocumentStore(string[] args)
        {
            string xml = File.ReadAllText(args[0]);
            XElement element = XElement.Parse(xml);
            DocumentStore.LanguageDefinition = AutosupportLanguageDefinition.FromXLinq(element, InterfaceDeserializer.Instance);
        }
    }
}
