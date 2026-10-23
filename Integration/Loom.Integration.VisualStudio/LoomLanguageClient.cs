using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Loom.Integration.VisualStudio;

[Export(typeof(ILanguageClient))]
[ContentType("Loom")]
public sealed class LoomLanguageClient : ILanguageClient
{
    /// <inheritdoc/>
    public string Name => "Loom";

    /// <inheritdoc/>
    public IEnumerable<string>? ConfigurationSections => null;

    /// <inheritdoc/>
    public object? InitializationOptions => null;

    /// <inheritdoc/>
    public IEnumerable<string>? FilesToWatch => null;

    /// <inheritdoc/>
    public bool ShowNotificationOnInitializeFailed => true;

    /// <inheritdoc/>
    public event AsyncEventHandler<EventArgs>? StartAsync;

    /// <inheritdoc/>
    public event AsyncEventHandler<EventArgs>? StopAsync { add { } remove { } }

    /// <inheritdoc/>
    public Task OnLoadedAsync() => StartAsync?.InvokeAsync(this, EventArgs.Empty) ?? Task.CompletedTask;

    /// <inheritdoc/>
    public Task<Connection?> ActivateAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var serverPath = Path.Combine(Path.GetDirectoryName(typeof(LoomLanguageClient).Assembly.Location)!, "Server", "Loom.Integration.LanguageServer.dll");
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{serverPath}\"",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }) ?? throw new InvalidOperationException("Could not start the Loom language server.");

        return Task.FromResult<Connection?>(new Connection(process.StandardOutput.BaseStream, process.StandardInput.BaseStream));
    }

    /// <inheritdoc/>
    public Task OnServerInitializedAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<InitializationFailureContext?> OnServerInitializeFailedAsync(ILanguageClientInitializationInfo initializationState) => Task.FromResult<InitializationFailureContext?>(null);
}