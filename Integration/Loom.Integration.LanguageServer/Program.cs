using Loom.Integration.LanguageServer.Handlers;

using Loom.Integration.LanguageServer.Documents;
using Microsoft.Extensions.DependencyInjection;

namespace Loom.Integration.LanguageServer;

internal class Program
{
    static async Task Main(string[] args)
    {
        var server = await OmniSharp.Extensions.LanguageServer.Server.LanguageServer.From(options => options
            .WithInput(Console.OpenStandardInput())
            .WithOutput(Console.OpenStandardOutput())
            .WithServices(services => services.AddSingleton<ParsedDocumentStore>())
            .WithHandler<TextDocumentSyncHandler>()
            .WithHandler<HoverHandler>()
        );

        await server.WaitForExit;
    }
}
