using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Loom.Integration.VisualStudio.Projects;

internal static class LoomProjectFiles
{
    public static IEnumerable<string> Enumerate(string directory)
    {
        foreach (string child in Directory.GetDirectories(directory).OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            if ((File.GetAttributes(child) & FileAttributes.ReparsePoint) != 0)
                continue;

            foreach (string file in Enumerate(child))
                yield return file;
        }

        foreach (string file in Directory.GetFiles(directory).Where(p => string.Equals(Path.GetExtension(p), ".loom", StringComparison.OrdinalIgnoreCase)).OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            yield return file;
    }
}
