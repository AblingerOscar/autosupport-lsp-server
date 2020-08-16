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
            if (!TrySetupDocumentStore(args, out IDocumentStore? documentStore, out string? error))
            {
                FailWithError("[ERROR]: Your language definition file is invalid: " + error ?? "unknown error");
                return;
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
                    .WithHandler<TextDocumentSyncHandler>()
                    .WithHandler<AutocompletionHandler>()
                    .WithHandler<ReferencesHandler>()
                    .WithHandler<DeclarationHandler>()
                    .WithHandler<DefinitionHandler>()
                    .WithHandler<ImplementationHandler>()
                    .WithHandler<FoldingHandler>()
                    ;
            });

            await server.WaitForExit;
        }

        private static void RegisterServices(IServiceCollection serviceCollection, IDocumentStore documentStore)
        {
            serviceCollection.AddSingleton(documentStore);
            serviceCollection.AddSingleton<ValidationHandler>();
        }

        private static bool TrySetupDocumentStore(string[] args, out IDocumentStore? documentStore, out string? error)
        {
            error = null;
            try
            {
                string xml = File.ReadAllText(args[0]);
                XElement element = XElement.Parse(xml, LoadOptions.PreserveWhitespace);
                documentStore = new DocumentStore(AutosupportLanguageDefinition.FromXLinq(element, InterfaceDeserializer.Instance));
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                documentStore = null;
                return false;
            }
        }

        private static async void FailWithError(string errorMsg)
        {
            var server = await LanguageServer.From(options =>
            {
                options.OnStarted((server, result) => {
                    server.SendNotification(errorMsg);
                    return Task.CompletedTask;
                });
            });

            await server.WaitForExit;
        }
    }
}
