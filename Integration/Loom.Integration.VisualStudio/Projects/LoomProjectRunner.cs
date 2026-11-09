using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Loom.Integration.VisualStudio.Projects;

internal static class LoomProjectRunner
{
    private static readonly Guid OutputPane = new("ac8da012-9ecb-41e0-837c-4406a35e8799");

    public static async Task RunAsync(Microsoft.VisualStudio.OLE.Interop.IServiceProvider site, IVsHierarchy hierarchy, string projectFile)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        using var services = new ServiceProvider(site);
        IVsOutputWindowPane? pane = null;
        try
        {
            if (services.GetService(typeof(SVsSolution)) is not IVsSolution solution)
                throw new InvalidOperationException("Visual Studio's solution service is unavailable.");

            int saved = solution.SaveSolutionElement((uint)__VSSLNSAVEOPTIONS.SLNSAVEOPT_SaveIfDirty, hierarchy, 0);
            if (saved == VSConstants.OLE_E_PROMPTSAVECANCELLED || saved == VSConstants.E_ABORT)
                return;

            ErrorHandler.ThrowOnFailure(saved);

            if (services.GetService(typeof(SVsOutputWindow)) is not IVsOutputWindow output)
                throw new InvalidOperationException("Visual Studio's Output window is unavailable.");

            Guid paneId = OutputPane;
            ErrorHandler.ThrowOnFailure(output.CreatePane(ref paneId, "Loom", 1, 0));
            ErrorHandler.ThrowOnFailure(output.GetPane(ref paneId, out pane));
            pane.Activate();

            if (services.GetService(typeof(SVsUIShell)) is IVsUIShell shell)
            {
                Guid window = VSConstants.StandardToolWindows.Output;
                if (ErrorHandler.Succeeded(shell.FindToolWindow(0, ref window, out var frame)))
                    frame?.Show();
            }

            pane.OutputStringThreadSafe($"loom run --path \"{projectFile}\"\r\n");
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "loom",
                    Arguments = $"run --path \"{projectFile}\"",
                    WorkingDirectory = Path.GetDirectoryName(projectFile)!,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.OutputDataReceived += (_, args) => WriteLine(pane, args.Data);
            process.ErrorDataReceived += (_, args) => WriteLine(pane, args.Data);

            if (!process.Start())
                throw new InvalidOperationException("Could not start the Loom CLI.");

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await Task.Run(() => process.WaitForExit());
            pane.OutputStringThreadSafe($"Loom CLI exited with code {process.ExitCode}.\r\n");
        }
        catch (Exception ex)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            string message = ex is Win32Exception { NativeErrorCode: 2 or 3 } ? "Could not find 'loom'. Install the Loom CLI and add it to PATH, then restart Visual Studio." : ex.Message;
            pane?.OutputStringThreadSafe($"Could not run the Loom project: {message}\r\n");
            VsShellUtilities.ShowMessageBox(services, message, "Run Loom Project", OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }

    private static void WriteLine(IVsOutputWindowPane pane, string? line)
    {
#pragma warning disable VSTHRD010
        if (line != null)
            pane.OutputStringThreadSafe(line + "\r\n");
#pragma warning restore VSTHRD010
    }
}
