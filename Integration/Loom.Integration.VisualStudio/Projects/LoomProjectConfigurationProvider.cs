using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Loom.Integration.VisualStudio.Projects;

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
public sealed class LoomProjectConfigurationProvider : IVsCfgProvider2, IVsProjectCfgProvider
{
    private readonly LoomProjectConfiguration[] configurations;
    private readonly Dictionary<uint, IVsCfgProviderEvents> sinks = [];
    private uint nextCookie = 1;

    public LoomProjectConfigurationProvider(string projectFile, Func<bool> canLaunch, Func<int> launch)
    {
        configurations =
        [
            new(this, "Debug", projectFile, canLaunch, launch),
            new(this, "Release", projectFile, canLaunch, launch)
        ];
    }

    public int GetCfgs(uint count, IVsCfg[] values, uint[] actual, uint[] flags)
    {
        if (flags is { Length: > 0 })
            flags[0] = 0;

        return Copy(configurations, count, values, actual);
    }

    public int GetCfgNames(uint count, string[] names, uint[] actual) => Copy(new[] { "Debug", "Release" }, count, names, actual);
    public int GetPlatformNames(uint count, string[] names, uint[] actual) => Copy(new[] { "x64" }, count, names, actual);
    public int GetSupportedPlatformNames(uint count, string[] names, uint[] actual) => Copy(new[] { "x64" }, count, names, actual);

    private static int Copy<T>(T[] source, uint count, T[] destination, uint[] actual)
    {
        if (count != 0 && destination == null)
            return VSConstants.E_POINTER;

        if (count > 0 && count > destination.Length)
            return VSConstants.E_INVALIDARG;

        uint copied = count == 0 ? (uint)source.Length : Math.Min(count, (uint)source.Length);
        if (actual is { Length: > 0 })
            actual[0] = copied;

        if (count != 0)
            Array.Copy(source, destination, (int)copied);

        return VSConstants.S_OK;
    }

    public int GetCfgOfName(string name, string platform, out IVsCfg configuration)
    {
        configuration = FindConfiguration(name, platform)!;
        return configuration == null ? VSConstants.E_INVALIDARG : VSConstants.S_OK;
    }

    private LoomProjectConfiguration? FindConfiguration(string name, string platform)
    {
        if (!string.IsNullOrEmpty(platform) && !string.Equals(platform, "x64", StringComparison.OrdinalIgnoreCase))
            return null;

        foreach (var candidate in configurations)
        {
            if (string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase))
                return candidate;
        }

        return null;
    }

    public int OpenProjectCfg(string canonicalName, out IVsProjectCfg configuration)
    {
        string[] parts = canonicalName.Split('|');
        configuration = parts.Length > 2 ? null! : FindConfiguration(parts[0], parts.Length == 2 ? parts[1] : string.Empty)!;
        return configuration == null ? VSConstants.E_INVALIDARG : VSConstants.S_OK;
    }

    public int get_UsesIndependentConfigurations(out int independent) { independent = 0; return VSConstants.S_OK; }

    public int GetCfgProviderProperty(int property, out object value)
    {
        value = false;
        return property == (int)__VSCFGPROPID.VSCFGPROPID_SupportsCfgAdd
            || property == (int)__VSCFGPROPID.VSCFGPROPID_SupportsCfgDelete
            || property == (int)__VSCFGPROPID.VSCFGPROPID_SupportsCfgRename
            || property == (int)__VSCFGPROPID.VSCFGPROPID_SupportsPlatformAdd
            || property == (int)__VSCFGPROPID.VSCFGPROPID_SupportsPlatformDelete
            || property == (int)__VSCFGPROPID.VSCFGPROPID_SupportsPrivateCfgs
            ? VSConstants.S_OK : VSConstants.E_NOTIMPL;
    }

    public int AdviseCfgProviderEvents(IVsCfgProviderEvents sink, out uint cookie)
    { cookie = nextCookie++; sinks.Add(cookie, sink); return VSConstants.S_OK; }
    public int UnadviseCfgProviderEvents(uint cookie) { sinks.Remove(cookie); return VSConstants.S_OK; }
    public int AddCfgsOfCfgName(string name, string clone, int isPrivate) => VSConstants.E_NOTIMPL;
    public int DeleteCfgsOfCfgName(string name) => VSConstants.E_NOTIMPL;
    public int RenameCfgsOfCfgName(string oldName, string newName) => VSConstants.E_NOTIMPL;
    public int AddCfgsOfPlatformName(string name, string clone) => VSConstants.E_NOTIMPL;
    public int DeleteCfgsOfPlatformName(string name) => VSConstants.E_NOTIMPL;
}

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
public sealed class LoomProjectConfiguration : IVsDebuggableProjectCfg
{
    private readonly LoomProjectConfigurationProvider provider;
    private readonly string projectFile;
    private readonly Func<bool> canLaunch;
    private readonly Func<int> launch;
    internal string Name { get; }

    internal LoomProjectConfiguration(LoomProjectConfigurationProvider provider, string name, string projectFile, Func<bool> canLaunch, Func<int> launch)
    {
        this.provider = provider;
        Name = name;
        this.projectFile = projectFile;
        this.canLaunch = canLaunch;
        this.launch = launch;
    }

    public int QueryDebugLaunch(uint flags, out int available) { available = canLaunch() ? 1 : 0; return VSConstants.S_OK; }
    public int DebugLaunch(uint flags) => canLaunch() ? launch() : VSConstants.E_UNEXPECTED;
    public int get_DisplayName(out string name) { name = Name + "|x64"; return VSConstants.S_OK; }
    public int get_CanonicalName(out string name) { name = Name + "|x64"; return VSConstants.S_OK; }
    public int get_IsDebugOnly(out int value) { value = 0; return VSConstants.S_OK; }
    public int get_IsReleaseOnly(out int value) { value = 0; return VSConstants.S_OK; }
    public int get_ProjectCfgProvider(out IVsProjectCfgProvider value) { value = provider; return VSConstants.S_OK; }
    public int get_BuildableProjectCfg(out IVsBuildableProjectCfg value) { value = null!; return VSConstants.E_NOTIMPL; }
    public int get_Platform(out Guid platform) { platform = Guid.Empty; return VSConstants.E_NOTIMPL; }
    public int get_IsPackaged(out int value) { value = 0; return VSConstants.S_OK; }
    public int get_IsSpecifyingOutputSupported(out int value) { value = 0; return VSConstants.S_OK; }
    public int get_TargetCodePage(out uint value) { value = 65001; return VSConstants.S_OK; }
    public int get_UpdateSequenceNumber(ULARGE_INTEGER[] value)
    {
        if (value is { Length: > 0 }) value[0] = default;
        return VSConstants.S_OK;
    }
    public int get_RootURL(out string url)
    {
        url = new Uri(Path.GetDirectoryName(projectFile)! + Path.DirectorySeparatorChar).AbsoluteUri;
        return VSConstants.S_OK;
    }
    public int EnumOutputs(out IVsEnumOutputs value) { value = null!; return VSConstants.E_NOTIMPL; }
    public int OpenOutput(string name, out IVsOutput value) { value = null!; return VSConstants.E_NOTIMPL; }
}