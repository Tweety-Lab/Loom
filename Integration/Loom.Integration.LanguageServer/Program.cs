using Loom.Integration.LanguageServer.Handlers;
using System.Diagnostics;

namespace Loom.Integration.LanguageServer;

internal class Program
{
    static async Task Main(string[] args)
    {
        var server = await OmniSharp.Extensions.LanguageServer.Server.LanguageServer.From(options => options
            .WithInput(Console.OpenStandardInput())
            .WithOutput(Console.OpenStandardOutput())
            .WithHandler<HoverHandler>()
        );

        await server.WaitForExit;
    }
}
