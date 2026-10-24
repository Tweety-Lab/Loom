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
            .WithServices(services => services
                .AddSingleton<ParsedDocumentStore>()
                .AddSingleton<WorkspaceDocumentStore>()
                .AddSingleton<DocumentAnalysisService>()
                .AddSingleton<DocumentDiagnosticService>())
            .WithHandler<TextDocumentSyncHandler>()
            .WithHandler<WorkspaceFilesHandler>()
            .WithHandler<HoverHandler>()
            .WithHandler<DefinitionHandler>()
        );

        await server.WaitForExit;
    }
}
