using System;
using System.IO;
using System.Linq;

namespace Loom.Integration.VisualStudio.Projects;

internal static class LoomProjectTemplate
{
    public static string Clone(string templateFile, string location, string name, bool overwrite)
    {
        string projectName = Path.GetFileName(name);
        if (!projectName.EndsWith(".lmproj", StringComparison.OrdinalIgnoreCase))
            projectName += ".lmproj";

        string destination = Path.GetFullPath(Path.Combine(location, projectName));
        string templateDirectory = Path.GetDirectoryName(Path.GetFullPath(templateFile))!;
        string[] sources = LoomProjectFiles.Enumerate(templateDirectory).ToArray();

        Directory.CreateDirectory(location);
        Copy(templateFile, destination, overwrite);

        foreach (string source in sources)
        {
            string relative = source.Substring(templateDirectory.TrimEnd(Path.DirectorySeparatorChar).Length + 1);
            string target = Path.Combine(location, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            Copy(source, target, overwrite);
        }

        return destination;
    }

    private static void Copy(string source, string destination, bool overwrite)
    {
        if (!string.Equals(Path.GetFullPath(source), Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
            File.Copy(source, destination, overwrite);
    }
}
