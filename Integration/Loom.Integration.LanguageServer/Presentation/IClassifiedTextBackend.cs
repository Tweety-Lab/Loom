using Loom.Integration.LanguageServer.Presentation.Generic;
using Loom.Integration.LanguageServer.Presentation.VisualStudio;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using LspRange = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Loom.Integration.LanguageServer.Presentation;

/// <summary>
/// Renders classified text in a client's supported presentation format.
/// </summary>
internal interface IClassifiedTextBackend
{
    private static readonly IClassifiedTextBackend generic = new GenericClassifiedTextBackend();
    private static readonly IClassifiedTextBackend visualStudio = new VisualStudioClassifiedTextBackend();

    Hover CreateHover(ClassifiedText text, LspRange? range = null);

    /// <summary> Selects the appropriate backend based on the client capabilities. </summary>
    public static IClassifiedTextBackend ForClient(ClientCapabilities capabilities)
        => capabilities.ExtensionData?.TryGetValue("_vs_supportsVisualStudioExtensions", out var supported) == true
        && supported.Type == JTokenType.Boolean && (bool)supported ? visualStudio : generic;
}