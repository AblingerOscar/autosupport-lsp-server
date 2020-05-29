using autosupport_lsp_server.LSP;
using autosupport_lsp_server.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
            if (!TrySetupDocumentStore(args, out IDocumentStore? documentStore))
            {
                Console.WriteLine("[ERROR]: The server could not be set up: There seems to be something wrong with your languageDefinition file");
                return; // TODO: somehow tell client that it failed and will always fail
            }

            if (documentStore == null)
                return; // should never happen, but this check is necessary for compilation with nullable enabled

            var server = await LanguageServer.From(options =>
            {
                options
                    .WithInput(Console.OpenStandardInput())
                    .WithOutput(Console.OpenStandardOutput())
                    .ConfigureLogging(lb =>
                        lb.AddLanguageServer()
                          .SetMinimumLevel(LogLevel.Trace))
                    .WithServices(serviceCollection =>
                        RegisterServices(serviceCollection, documentStore))
                    .WithHandler<LSP.TextDocumentSyncHandler>()
                    .WithHandler<KeywordsCompletetionHandler>()
                    ;
            });

            await server.WaitForExit;
        }

        private static void RegisterServices(IServiceCollection serviceCollection, IDocumentStore documentStore)
        {
            serviceCollection.AddSingleton(documentStore);
        }

        private static bool TrySetupDocumentStore(string[] args, out IDocumentStore? documentStore)
        {
            try
            {
                string xml = File.ReadAllText(args[0]);
                XElement element = XElement.Parse(xml);
                documentStore = new DocumentStore(AutosupportLanguageDefinition.FromXLinq(element, InterfaceDeserializer.Instance));
                return true;
            }
            catch (Exception)
            {
                documentStore = null;
                return false;
            }
        }
    }
}
