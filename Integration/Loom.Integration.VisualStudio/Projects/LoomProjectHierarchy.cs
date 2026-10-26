using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using OleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using OleConstants = Microsoft.VisualStudio.OLE.Interop.Constants;

namespace Loom.Integration.VisualStudio.Projects;

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
public sealed class LoomProjectHierarchy : IVsUIHierarchy, IVsProject, IPersistFileFormat, IOleCommandTarget
{
    private sealed class Node(uint id, string path, uint parent, bool folder)
    {
        public uint Id { get; } = id;
        public string Path { get; } = path;
        public uint Parent { get; } = parent;
        public bool Folder { get; } = folder;
        public List<uint> Children { get; } = [];
    }

    private readonly Dictionary<uint, Node> nodes = [];
    private readonly Dictionary<string, uint> ids = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<uint, IVsHierarchyEvents> sinks = [];
    private readonly Dictionary<int, object> rootProperties = [];
    private readonly HashSet<uint> expandedItems = [VSConstants.VSITEMID_ROOT];
    private readonly string projectFile;
    private readonly string directory;
    private readonly OleServiceProvider packageSite;

    private OleServiceProvider? site;
    private OleServiceProvider Site => site ?? packageSite;
    private FileSystemWatcher? watcher;
    private uint nextId = 1;
    private uint nextCookie = 1;
    private Guid projectId = Guid.NewGuid();
    private bool closed;
    private uint contextItem = VSConstants.VSITEMID_ROOT;

    public LoomProjectHierarchy(string filename, OleServiceProvider site)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        projectFile = Path.GetFullPath(filename);
        directory = Path.GetDirectoryName(projectFile)!;
        packageSite = site ?? throw new ArgumentNullException(nameof(site));
        Refresh();

        watcher = new FileSystemWatcher(directory)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName
        };

        watcher.Created += OnFilesChanged;
        watcher.Deleted += OnFilesChanged;
        watcher.Renamed += OnFilesChanged;
        watcher.Error += OnWatcherError;
        watcher.EnableRaisingEvents = true;
    }

#pragma warning disable VSSDK007
#pragma warning disable VSTHRD010
    private void OnFilesChanged(object sender, FileSystemEventArgs args) => QueueRefresh();
    private void OnWatcherError(object sender, ErrorEventArgs args) => QueueRefresh();

    private void QueueRefresh()
    {
        ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (!closed)
                Refresh();
        }).FileAndForget("Loom/ProjectRefresh");
    }
#pragma warning restore VSSDK007
#pragma warning restore VSTHRD010

    private void Refresh()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        var updated = new Dictionary<uint, Node>();
        var root = new Node(VSConstants.VSITEMID_ROOT, projectFile, VSConstants.VSITEMID_NIL, true);
        updated.Add(root.Id, root);
        try
        {
            var folders = new Dictionary<string, Node>(StringComparer.OrdinalIgnoreCase) { [directory] = root };
            Node GetParent(string path)
            {
                if (folders.TryGetValue(path, out var folder))
                    return folder;

                Node parent = GetParent(Path.GetDirectoryName(path)!);
                folder = MakeNode(path, parent.Id, true);
                folders.Add(path, folder);
                updated.Add(folder.Id, folder);
                parent.Children.Add(folder.Id);

                return folder;
            }
            foreach (string file in LoomProjectFiles.Enumerate(directory))
            {
                Node parent = GetParent(Path.GetDirectoryName(file)!);
                Node node = MakeNode(file, parent.Id, false);
                updated.Add(node.Id, node);
                parent.Children.Add(node.Id);
            }
        }
        catch (IOException) { return; }
        catch (UnauthorizedAccessException) { return; }

        nodes.Clear();
        foreach (var pair in updated)
            nodes.Add(pair.Key, pair.Value);

        foreach (var sink in sinks.Values.ToArray())
            sink.OnInvalidateItems(VSConstants.VSITEMID_ROOT);
    }

    private Node MakeNode(string path, uint parent, bool folder)
    {
        if (!ids.TryGetValue(path, out uint id))
            ids.Add(path, id = nextId++);

        return new Node(id, path, parent, folder);
    }

    /// <inheritdoc/>
    public int SetSite(OleServiceProvider serviceProvider) { site = serviceProvider; return VSConstants.S_OK; }

    /// <inheritdoc/>
    public int GetSite(out OleServiceProvider serviceProvider) { serviceProvider = Site; return VSConstants.S_OK; }

    /// <inheritdoc/>
    public int QueryClose(out int canClose) { canClose = 1; return VSConstants.S_OK; }

    /// <inheritdoc/>
    public int Close()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        closed = true;
        watcher?.Dispose();
        watcher = null;
        site = null;
        sinks.Clear();

        return VSConstants.S_OK;
    }

    /// <inheritdoc/>
    public int GetProperty(uint itemid, int propid, out object value)
    {
        value = null!;
        if (!nodes.TryGetValue(itemid, out var node))
            return VSConstants.E_INVALIDARG;

        bool root = itemid == VSConstants.VSITEMID_ROOT;

        if (root && (propid == (int)__VSHPROPID.VSHPROPID_Caption || propid == (int)__VSHPROPID.VSHPROPID_ProjectName))
        { value = Path.GetFileNameWithoutExtension(projectFile); return VSConstants.S_OK; }

        var icon = root ? KnownMonikers.Application : node.Folder ? KnownMonikers.FolderClosed : KnownMonikers.DocumentSource;
        if (propid == (int)__VSHPROPID8.VSHPROPID_SupportsIconMonikers)
        { value = true; return VSConstants.S_OK; }

        if (propid == (int)__VSHPROPID8.VSHPROPID_IconMonikerGuid || propid == (int)__VSHPROPID8.VSHPROPID_OpenFolderIconMonikerGuid)
        { value = icon.Guid; return VSConstants.S_OK; }

        if (propid == (int)__VSHPROPID8.VSHPROPID_IconMonikerId)
        { value = icon.Id; return VSConstants.S_OK; }

        if (propid == (int)__VSHPROPID8.VSHPROPID_OpenFolderIconMonikerId)
        { value = root || !node.Folder ? icon.Id : KnownMonikers.FolderOpened.Id; return VSConstants.S_OK; }

        if (root && rootProperties.TryGetValue(propid, out value))
            return VSConstants.S_OK;

        switch ((__VSHPROPID)propid)
        {
            case __VSHPROPID.VSHPROPID_Root: value = this; break;
            case __VSHPROPID.VSHPROPID_Caption:
            case __VSHPROPID.VSHPROPID_ProjectName: value = root ? Path.GetFileNameWithoutExtension(projectFile) : Path.GetFileName(node.Path); break;
            case __VSHPROPID.VSHPROPID_SaveName: value = node.Path; break;
            case __VSHPROPID.VSHPROPID_ProjectDir: value = directory + Path.DirectorySeparatorChar; break;
            case __VSHPROPID.VSHPROPID_TypeName: value = root ? "Loom" : node.Folder ? "Folder" : "Loom file"; break;
            case __VSHPROPID.VSHPROPID_Parent: value = unchecked((int)node.Parent); break;
            case __VSHPROPID.VSHPROPID_FirstChild:
            case __VSHPROPID.VSHPROPID_FirstVisibleChild: value = unchecked((int)(node.Children.Count == 0 ? VSConstants.VSITEMID_NIL : node.Children[0])); break;
            case __VSHPROPID.VSHPROPID_NextSibling:
            case __VSHPROPID.VSHPROPID_NextVisibleSibling:
                var siblings = root ? null : nodes[node.Parent].Children;
                int index = siblings?.IndexOf(itemid) ?? -1;
                value = unchecked((int)(siblings != null && index + 1 < siblings.Count ? siblings[index + 1] : VSConstants.VSITEMID_NIL));
                break;
            case __VSHPROPID.VSHPROPID_Expandable: value = node.Children.Count > 0; break;
            case __VSHPROPID.VSHPROPID_ExpandByDefault: value = root; break;
            case __VSHPROPID.VSHPROPID_Expanded: value = expandedItems.Contains(itemid); break;
            case __VSHPROPID.VSHPROPID_IsNonMemberItem:
            case __VSHPROPID.VSHPROPID_IsHiddenItem:
            case __VSHPROPID.VSHPROPID_DefaultEnableBuildProjectCfg:
            case __VSHPROPID.VSHPROPID_DefaultEnableDeployProjectCfg: value = false; break;
            default: return VSConstants.DISP_E_MEMBERNOTFOUND;
        }

        return VSConstants.S_OK;
    }

    /// <inheritdoc/>
    public int SetProperty(uint itemid, int propid, object value)
    {
        if (propid == (int)__VSHPROPID.VSHPROPID_Expanded && nodes.ContainsKey(itemid))
        {
            if (Convert.ToBoolean(value)) expandedItems.Add(itemid);
            else expandedItems.Remove(itemid);
            return VSConstants.S_OK;
        }

        if (itemid != VSConstants.VSITEMID_ROOT)
            return VSConstants.E_NOTIMPL;

        rootProperties[propid] = value;
        return VSConstants.S_OK;
    }

    /// <inheritdoc/>
    public int GetGuidProperty(uint itemid, int propid, out Guid guid)
    {
        guid = Guid.Empty;
        if (!nodes.TryGetValue(itemid, out var node))
            return VSConstants.E_INVALIDARG;

        if (propid == (int)__VSHPROPID8.VSHPROPID_IconMonikerGuid || propid == (int)__VSHPROPID8.VSHPROPID_OpenFolderIconMonikerGuid)
            guid = KnownMonikers.Application.Guid;
        else if (propid == (int)__VSHPROPID.VSHPROPID_TypeGuid)
            guid = itemid == VSConstants.VSITEMID_ROOT ? new Guid(LoomProjectFactory.ProjectTypeGuid) : node.Folder ? VSConstants.GUID_ItemType_PhysicalFolder : VSConstants.GUID_ItemType_PhysicalFile;
        else if (propid == (int)__VSHPROPID.VSHPROPID_ProjectIDGuid)
            guid = projectId;
        else if (propid == (int)__VSHPROPID.VSHPROPID_CmdUIGuid)
            guid = new Guid(LoomProjectFactory.ProjectTypeGuid);
        else
            return VSConstants.DISP_E_MEMBERNOTFOUND;

        return VSConstants.S_OK;
    }

    /// <inheritdoc/>
    public int SetGuidProperty(uint itemid, int propid, ref Guid guid)
    {
        if (itemid != VSConstants.VSITEMID_ROOT || propid != (int)__VSHPROPID.VSHPROPID_ProjectIDGuid)
            return VSConstants.E_NOTIMPL;

        projectId = guid;
        return VSConstants.S_OK;
    }

    /// <inheritdoc/>
    public int GetCanonicalName(uint itemid, out string name)
    {
        name = nodes.TryGetValue(itemid, out var node) ? node.Path : null!;
        return name == null ? VSConstants.E_INVALIDARG : VSConstants.S_OK;
    }

    /// <inheritdoc/>
    public int ParseCanonicalName(string name, out uint itemid)
    {
        itemid = VSConstants.VSITEMID_NIL;
        string path = Path.GetFullPath(name);

        if (string.Equals(path, projectFile, StringComparison.OrdinalIgnoreCase))
            itemid = VSConstants.VSITEMID_ROOT;
        else if (ids.TryGetValue(path, out uint id) && nodes.ContainsKey(id))
            itemid = id;

        return itemid == VSConstants.VSITEMID_NIL ? VSConstants.E_FAIL : VSConstants.S_OK;
    }

    /// <inheritdoc/>
    public int GetNestedHierarchy(uint itemid, ref Guid iid, out IntPtr hierarchy, out uint nestedItem)
    { hierarchy = IntPtr.Zero; nestedItem = VSConstants.VSITEMID_NIL; return VSConstants.E_NOTIMPL; }

    /// <inheritdoc/>
    public int AdviseHierarchyEvents(IVsHierarchyEvents sink, out uint cookie)
    { cookie = nextCookie++; sinks.Add(cookie, sink); return VSConstants.S_OK; }

    /// <inheritdoc/>
    public int UnadviseHierarchyEvents(uint cookie) { sinks.Remove(cookie); return VSConstants.S_OK; }

    // ???
    public int Unused0() => VSConstants.E_NOTIMPL;
    public int Unused1() => VSConstants.E_NOTIMPL;
    public int Unused2() => VSConstants.E_NOTIMPL;
    public int Unused3() => VSConstants.E_NOTIMPL;
    public int Unused4() => VSConstants.E_NOTIMPL;

    /// <inheritdoc/>
    public int IsDocumentInProject(string name, out int found, VSDOCUMENTPRIORITY[] priority, out uint itemid)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        found = ErrorHandler.Succeeded(ParseCanonicalName(name, out itemid)) ? 1 : 0;
        if (priority != null && priority.Length > 0)
            priority[0] = VSDOCUMENTPRIORITY.DP_Standard;

        return VSConstants.S_OK;
    }

    /// <inheritdoc/>
    public int GetMkDocument(uint itemid, out string name)
    { ThreadHelper.ThrowIfNotOnUIThread(); return GetCanonicalName(itemid, out name); }

    /// <inheritdoc/>
    public int GetItemContext(uint itemid, out OleServiceProvider serviceProvider)
    { ThreadHelper.ThrowIfNotOnUIThread(); return GetSite(out serviceProvider); }

    /// <inheritdoc/>
    public int OpenItem(uint itemid, ref Guid logicalView, IntPtr existingDocData, out IVsWindowFrame frame)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        frame = null!;
        if (!nodes.TryGetValue(itemid, out var node) || node.Folder)
            return VSConstants.E_INVALIDARG;

        using var services = new ServiceProvider(Site);
        if (services.GetService(typeof(SVsUIShellOpenDocument)) is not IVsUIShellOpenDocument opener)
            return VSConstants.E_NOINTERFACE;

        Guid editor = VSConstants.VsEditorFactoryGuid.TextEditor_guid;
        int result = opener.OpenSpecificEditor(0, node.Path, ref editor, null, ref logicalView, Path.GetFileName(node.Path), this, itemid, existingDocData, Site, out frame);
        if (ErrorHandler.Succeeded(result))
            frame.Show();

        return result;
    }

    /// <inheritdoc/>
    public int GenerateUniqueItemName(uint itemid, string extension, string suggested, out string name)
    {
        string folder = GetFolder(itemid);
        string stem = Path.GetFileNameWithoutExtension(suggested);

        if (string.IsNullOrEmpty(stem))
            stem = "NewFile";

        extension = extension.StartsWith(".", StringComparison.Ordinal) ? extension : "." + extension;
        name = stem + extension;

        for (int i = 1; File.Exists(Path.Combine(folder, name)); i++)
            name = stem + i + extension;

        return VSConstants.S_OK;
    }

    private string GetFolder(uint itemid) => nodes.TryGetValue(itemid, out var node) && node.Folder && itemid != VSConstants.VSITEMID_ROOT ? node.Path : directory;

    /// <inheritdoc/>
    public int AddItem(uint itemid, VSADDITEMOPERATION operation, string name, uint count, string[] files, IntPtr owner, VSADDRESULT[] result)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        result[0] = VSADDRESULT.ADDRESULT_Failure;
        if (operation != VSADDITEMOPERATION.VSADDITEMOP_OPENFILE && operation != VSADDITEMOPERATION.VSADDITEMOP_CLONEFILE)
            return VSConstants.E_NOTIMPL;

        try
        {
            for (int i = 0; i < count; i++)
            {
                if (!string.Equals(Path.GetExtension(files[i]), ".loom", StringComparison.OrdinalIgnoreCase))
                    return VSConstants.E_INVALIDARG;

                string destination = Path.Combine(GetFolder(itemid), operation == VSADDITEMOPERATION.VSADDITEMOP_CLONEFILE ? Path.GetFileName(name) : Path.GetFileName(files[i]));
                if (!string.Equals(Path.GetExtension(destination), ".loom", StringComparison.OrdinalIgnoreCase))
                    return VSConstants.E_INVALIDARG;

                if (!string.Equals(Path.GetFullPath(files[i]), destination, StringComparison.OrdinalIgnoreCase))
                    File.Copy(files[i], destination, overwrite: false);
            }

            Refresh();
            result[0] = VSADDRESULT.ADDRESULT_Success;
            return VSConstants.S_OK;
        }
        catch (IOException ex) { return Marshal.GetHRForException(ex); }
        catch (UnauthorizedAccessException ex) { return Marshal.GetHRForException(ex); }
    }

    /// <inheritdoc/>
    public int QueryStatusCommand(uint itemid, ref Guid group, uint count, OLECMD[] commands, IntPtr text)
    {
        if (group != VSConstants.GUID_VSStandardCommandSet97)
            return (int)OleConstants.OLECMDERR_E_UNKNOWNGROUP;

        for (int i = 0; i < count; i++)
        {
            if (nodes.TryGetValue(itemid, out var node) &&
                (commands[i].cmdID == (uint)VSConstants.VSStd97CmdID.Open && !node.Folder || commands[i].cmdID == (uint)VSConstants.VSStd97CmdID.AddExistingItem && node.Folder))
                commands[i].cmdf = (uint)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED);
        }

        return VSConstants.S_OK;
    }

    /// <inheritdoc/>
    public int ExecCommand(uint itemid, ref Guid group, uint command, uint options, IntPtr input, IntPtr output)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (group == VSConstants.GUID_VsUIHierarchyWindowCmds && command == (uint)VSConstants.VsUIHierarchyWindowCmdIds.UIHWCMDID_RightClick)
        {
            contextItem = itemid;
            using var services = new ServiceProvider(Site);
            if (services.GetService(typeof(SVsUIShell)) is not IVsUIShell shell)
                return VSConstants.E_NOINTERFACE;

            int position = (int)Marshal.GetObjectForNativeVariant(input);
            var points = new[] { new POINTS { x = unchecked((short)(position & 0xffff)), y = unchecked((short)((position >> 16) & 0xffff)) } };
            Guid menu = VsMenus.guidSHLMainMenu;
            int menuId = itemid == VSConstants.VSITEMID_ROOT ? VsMenus.IDM_VS_CTXT_PROJNODE : nodes[itemid].Folder ? VsMenus.IDM_VS_CTXT_FOLDERNODE : VsMenus.IDM_VS_CTXT_ITEMNODE;

            return shell.ShowContextMenu(0, ref menu, menuId, points, this);
        }
        if (group == VSConstants.GUID_VsUIHierarchyWindowCmds && (command == (uint)VSConstants.VsUIHierarchyWindowCmdIds.UIHWCMDID_DoubleClick || command == (uint)VSConstants.VsUIHierarchyWindowCmdIds.UIHWCMDID_EnterKey)
            || group == VSConstants.GUID_VSStandardCommandSet97 && command == (uint)VSConstants.VSStd97CmdID.Open)
        {
            if (nodes.TryGetValue(itemid, out var node) && node.Folder)
            {
                SetProperty(itemid, (int)__VSHPROPID.VSHPROPID_Expanded, !expandedItems.Contains(itemid));
                foreach (var sink in sinks.Values.ToArray())
                    sink.OnPropertyChanged(itemid, (int)__VSHPROPID.VSHPROPID_Expanded, 0);

                return VSConstants.S_OK;
            }

            Guid view = VSConstants.LOGVIEWID_Primary;
            return OpenItem(itemid, ref view, IntPtr.Zero, out _);
        }
        if (group == VSConstants.GUID_VSStandardCommandSet97 && command == (uint)VSConstants.VSStd97CmdID.AddExistingItem)
        {
            using var services = new ServiceProvider(Site);
            if (services.GetService(typeof(SVsAddProjectItemDlg)) is not IVsAddProjectItemDlg dialog)
                return VSConstants.E_NOINTERFACE;

            Guid type = new(LoomProjectFactory.ProjectTypeGuid);
            string location = GetFolder(itemid);
            string filter = "Loom files (*.loom)|*.loom";

            return dialog.AddProjectItemDlg(itemid, ref type, this, (uint)(__VSADDITEMFLAGS.VSADDITEM_AddExistingItems | __VSADDITEMFLAGS.VSADDITEM_AllowMultiSelect), null, null, ref location, ref filter, out _);
        }
        return (int)OleConstants.OLECMDERR_E_NOTSUPPORTED;
    }

    /// <inheritdoc/>
    public int QueryStatus(ref Guid group, uint count, OLECMD[] commands, IntPtr text)
    { ThreadHelper.ThrowIfNotOnUIThread(); return QueryStatusCommand(contextItem, ref group, count, commands, text); }

    /// <inheritdoc/>
    public int Exec(ref Guid group, uint command, uint options, IntPtr input, IntPtr output)
    { ThreadHelper.ThrowIfNotOnUIThread(); return ExecCommand(contextItem, ref group, command, options, input, output); }

    public int GetClassID(out Guid guid) { guid = new Guid(LoomProjectFactory.ProjectTypeGuid); return VSConstants.S_OK; }
    public int IsDirty(out int dirty) { dirty = 0; return VSConstants.S_OK; }
    public int InitNew(uint format) => VSConstants.E_NOTIMPL;
    public int Load(string filename, uint mode, int readOnly) => string.Equals(Path.GetFullPath(filename), projectFile, StringComparison.OrdinalIgnoreCase) ? VSConstants.S_OK : VSConstants.E_INVALIDARG;
    public int Save(string filename, int remember, uint format) => string.IsNullOrEmpty(filename) || string.Equals(Path.GetFullPath(filename), projectFile, StringComparison.OrdinalIgnoreCase) ? VSConstants.S_OK : VSConstants.E_NOTIMPL;
    public int SaveCompleted(string filename) => VSConstants.S_OK;
    public int GetCurFile(out string filename, out uint format) { filename = projectFile; format = 0; return VSConstants.S_OK; }
    public int GetFormatList(out string formats) { formats = "Loom Projects (*.lmproj)\n*.lmproj\n"; return VSConstants.S_OK; }
}
