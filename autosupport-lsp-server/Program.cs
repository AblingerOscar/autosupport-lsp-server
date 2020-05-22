using autosupport_lsp_server.LSP;
using autosupport_lsp_server.Serialization;
using Microsoft.Extensions.DependencyInjection;
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
            if (!TrySetupDocumentStore(args))
            {
                return; // TODO: somehow tell client that it failed and will always fail
            }

            var server = await LanguageServer.From(options =>
            {
                options
                    .WithInput(Console.OpenStandardInput())
                    .WithOutput(Console.OpenStandardOutput())
                    .WithHandler<TextDocumentSyncHandler>()
                    .WithServices(RegisterServices);
            });

            await server.WaitForExit;
        }

        private static void RegisterServices(IServiceCollection collection)
        {
            collection.AddSingleton<KeywordsCompletetionHandler>();
        }

        static private bool TrySetupDocumentStore(string[] args)
        {
            try
            {
                string xml = File.ReadAllText(args[0]);
                XElement element = XElement.Parse(xml);
                DocumentStore.LanguageDefinition = AutosupportLanguageDefinition.FromXLinq(element, InterfaceDeserializer.Instance);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
