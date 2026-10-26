using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using OleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Loom.Integration.VisualStudio.Projects;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[Guid("FC388F62-FA15-4537-9CC4-383492DAD912")]
[ComVisible(true)]
[ProvideProjectFactory(typeof(LoomProjectFactory), null, "Loom Projects (*.lmproj);*.lmproj", "lmproj", "lmproj", ".\\NullPath", LanguageVsTemplate = "Loom")]
public sealed class LoomProjectPackage : AsyncPackage
{
    /// <inheritdoc/>
    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        if (await GetServiceAsync(typeof(OleServiceProvider)) is not OleServiceProvider serviceProvider)
            throw new InvalidOperationException("Visual Studio's OLE service provider is unavailable.");

        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        RegisterProjectFactory(new LoomProjectFactory(serviceProvider));
    }
}

[Guid(ProjectTypeGuid)]
[ComVisible(true)]
public sealed class LoomProjectFactory : IVsProjectFactory
{
    public const string ProjectTypeGuid = "fb8a16cb-22d3-4b29-a4cb-1c97627660dd";
    private readonly OleServiceProvider packageSite;
    private OleServiceProvider? site;

    public LoomProjectFactory(OleServiceProvider serviceProvider)
    {
        packageSite = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc/>
    public int SetSite(OleServiceProvider serviceProvider) { site = serviceProvider; return VSConstants.S_OK; }

    /// <inheritdoc/>
    public int Close() { site = null; return VSConstants.S_OK; }

    /// <inheritdoc/>
    public int CanCreateProject(string filename, uint flags, out int canCreate)
    {
        bool fromTemplate = (flags & (uint)__VSCREATEPROJFLAGS.CPF_CLONEFILE) != 0;
        canCreate = fromTemplate || string.Equals(Path.GetExtension(filename), ".lmproj", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        return VSConstants.S_OK;
    }

    /// <inheritdoc/>
    public int CreateProject(string filename, string location, string name, uint flags, ref Guid iid, out IntPtr project, out int canceled)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        project = IntPtr.Zero;
        canceled = 0;
        if (!File.Exists(filename))
            return VSConstants.E_INVALIDARG;

        if ((flags & (uint)__VSCREATEPROJFLAGS.CPF_CLONEFILE) != 0)
        {
            try
            {
                filename = LoomProjectTemplate.Clone(filename, location, name, (flags & (uint)__VSCREATEPROJFLAGS.CPF_OVERWRITE) != 0);
            }
            catch (IOException ex) { return Marshal.GetHRForException(ex); }
            catch (UnauthorizedAccessException ex) { return Marshal.GetHRForException(ex); }
        }

        var hierarchy = new LoomProjectHierarchy(filename, site ?? packageSite);
        IntPtr unknown = Marshal.GetIUnknownForObject(hierarchy);
        try
        {
            int result = Marshal.QueryInterface(unknown, ref iid, out project);
            if (ErrorHandler.Failed(result))
                hierarchy.Close();
            return result;
        }
        finally { Marshal.Release(unknown); }
    }
}
