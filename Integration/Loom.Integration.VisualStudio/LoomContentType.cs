using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace Loom.Integration.VisualStudio;

internal static class LoomContentType
{
    [Export]
    [Name("Loom")]
    [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteContentTypeName)]
    internal static ContentTypeDefinition Definition = null!;

    [Export]
    [FileExtension(".loom")]
    [ContentType("Loom")]
    internal static FileExtensionToContentTypeDefinition FileExtension = null!;
}