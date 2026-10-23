using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using Loom.Integration.LanguageServer.Presentation.VisualStudio;
using Loom.Integration.LanguageServer.Presentation.Generic;

namespace Loom.Integration.LanguageServer.Presentation;

internal static class ClassifiedTextBackend
{
    private static readonly IClassifiedTextBackend generic = new GenericClassifiedTextBackend();
    private static readonly IClassifiedTextBackend visualStudio = new VisualStudioClassifiedTextBackend();

    /// <summary> Selects the appropriate backend based on the client capabilities. </summary>
    public static IClassifiedTextBackend ForClient(ClientCapabilities capabilities)
        => capabilities.ExtensionData?.TryGetValue("_vs_supportsVisualStudioExtensions", out var supported) == true
        && supported.Type == JTokenType.Boolean && (bool)supported ? visualStudio : generic;
}