using OmniSharp.Extensions.LanguageServer.Server;
using System;
using System.Threading.Tasks;

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
            // TODO: instead have a path to the file that will be deserialized
            DocumentStore.LanguageDefinition = new AutosupportLanguageDefinition(args[0], args[1]);
        }
    }
}
