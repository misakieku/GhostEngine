using Ghost.AssetForge.Core.Models;
using Ghost.AssetForge.Core.Services;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Ghost.Studio.Platform;

/// <summary>
/// Native Windows project dialogs used before engine startup and from the File menu.
/// </summary>
[SupportedOSPlatform("windows")]
internal static unsafe class NativeProjectDialogs
{
    private const int BUTTON_OPEN_PROJECT = 1001;
    private const int BUTTON_CREATE_PROJECT = 1002;
    private const int BUTTON_EXIT = 2;

    private const int TDF_ALLOW_DIALOG_CANCELLATION = 0x0008;
    private const int TDF_USE_COMMAND_LINKS = 0x0010;
    private const int TDF_POSITION_RELATIVE_TO_WINDOW = 0x1000;

    private const uint OFN_OVERWRITEPROMPT = 0x00000002;
    private const uint OFN_NOCHANGEDIR = 0x00000008;
    private const uint OFN_PATHMUSTEXIST = 0x00000800;
    private const uint OFN_FILEMUSTEXIST = 0x00001000;
    private const uint OFN_EXPLORER = 0x00080000;

    private const uint MB_OK = 0x00000000;
    private const uint MB_YESNOCANCEL = 0x00000003;
    private const uint MB_ICONERROR = 0x00000010;
    private const uint MB_ICONQUESTION = 0x00000020;
    private const int IDYES = 6;
    private const int IDNO = 7;

    public enum UnsavedPromptResult
    {
        Save,
        Discard,
        Cancel
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct TASKDIALOG_BUTTON
    {
        public int nButtonID;
        public char* pszButtonText;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct TASKDIALOGCONFIG
    {
        public uint cbSize;
        public nint hwndParent;
        public nint hInstance;
        public int dwFlags;
        public int dwCommonButtons;
        public char* pszWindowTitle;
        public nint hMainIcon;
        public char* pszMainInstruction;
        public char* pszContent;
        public uint cButtons;
        public TASKDIALOG_BUTTON* pButtons;
        public int nDefaultButton;
        public uint cRadioButtons;
        public nint pRadioButtons;
        public int nDefaultRadioButton;
        public char* pszVerificationText;
        public char* pszExpandedInformation;
        public char* pszExpandedControlText;
        public char* pszCollapsedControlText;
        public nint hFooterIcon;
        public char* pszFooter;
        public nint pfCallback;
        public nint lpCallbackData;
        public uint cxWidth;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct OPENFILENAMEW
    {
        public uint lStructSize;
        public nint hwndOwner;
        public nint hInstance;
        public char* lpstrFilter;
        public char* lpstrCustomFilter;
        public uint nMaxCustFilter;
        public uint nFilterIndex;
        public char* lpstrFile;
        public uint nMaxFile;
        public char* lpstrFileTitle;
        public uint nMaxFileTitle;
        public char* lpstrInitialDir;
        public char* lpstrTitle;
        public uint Flags;
        public ushort nFileOffset;
        public ushort nFileExtension;
        public char* lpstrDefExt;
        public nint lCustData;
        public nint lpfnHook;
        public char* lpTemplateName;
        public nint pvReserved;
        public uint dwReserved;
        public uint FlagsEx;
    }

    [DllImport("comctl32.dll", ExactSpelling = true)]
    private static extern int TaskDialogIndirect(TASKDIALOGCONFIG* pTaskConfig, int* pnButton, int* pnRadioButton, int* pfVerificationFlagChecked);

    [DllImport("comdlg32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetOpenFileNameW(OPENFILENAMEW* lpofn);

    [DllImport("comdlg32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSaveFileNameW(OPENFILENAMEW* lpofn);

    [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(nint hWnd, string lpText, string lpCaption, uint uType);

    public static bool PromptStartupProject(ProjectService projectService)
    {
        while (true)
        {
            var choice = ShowStartupChoiceDialog();
            if (choice == BUTTON_OPEN_PROJECT)
            {
                if (TryOpenProject(projectService))
                {
                    return true;
                }
            }
            else if (choice == BUTTON_CREATE_PROJECT)
            {
                if (TryCreateProject(projectService))
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }
    }

    public static bool TryOpenProject(ProjectService projectService, nint hwndOwner = 0)
    {
        var selectedPath = ShowOpenProjectFileDialog(hwndOwner);
        if (string.IsNullOrEmpty(selectedPath))
        {
            return false;
        }

        try
        {
            projectService.OpenProject(selectedPath);
            return true;
        }
        catch (Exception ex)
        {
            ShowError(hwndOwner, "Failed to Open Project", ex.Message);
            return false;
        }
    }

    public static bool TryCreateProject(ProjectService projectService, nint hwndOwner = 0)
    {
        var selectedPath = ShowCreateProjectSaveDialog(hwndOwner);
        if (string.IsNullOrEmpty(selectedPath))
        {
            return false;
        }

        try
        {
            var projectName = Path.GetFileNameWithoutExtension(selectedPath);
            if (string.IsNullOrWhiteSpace(projectName))
            {
                projectName = "NewGhostProject";
            }

            var selectedDir = Path.GetDirectoryName(selectedPath) ?? Environment.CurrentDirectory;
            var folderLeafName = Path.GetFileName(selectedDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            var projectRoot = string.Equals(folderLeafName, projectName, StringComparison.OrdinalIgnoreCase)
                ? selectedDir
                : Path.Combine(selectedDir, projectName);

            projectService.CreateProject(projectRoot, projectName);
            return true;
        }
        catch (Exception ex)
        {
            ShowError(hwndOwner, "Failed to Create Project", ex.Message);
            return false;
        }
    }

    private static int ShowStartupChoiceDialog()
    {
        fixed (char* pTitle = "GhostEngine Studio")
        fixed (char* pInstruction = "Select or create a GhostEngine project")
        fixed (char* pContent = $"Choose an existing {Project.EXTENSION} project to open, or pick a location and name to generate a new project.")
        fixed (char* pOpenText = $"Open Existing Project...\nBrowse for a {Project.EXTENSION} project file")
        fixed (char* pCreateText = $"Create New Project...\nGenerate a new {Project.EXTENSION} project and C# game assembly")
        {
            var buttons = stackalloc TASKDIALOG_BUTTON[2];
            buttons[0] = new TASKDIALOG_BUTTON { nButtonID = BUTTON_OPEN_PROJECT, pszButtonText = pOpenText };
            buttons[1] = new TASKDIALOG_BUTTON { nButtonID = BUTTON_CREATE_PROJECT, pszButtonText = pCreateText };

            var config = new TASKDIALOGCONFIG
            {
                cbSize = (uint)sizeof(TASKDIALOGCONFIG),
                dwFlags = TDF_USE_COMMAND_LINKS | TDF_ALLOW_DIALOG_CANCELLATION | TDF_POSITION_RELATIVE_TO_WINDOW,
                dwCommonButtons = 0x0008, // TDCBF_CANCEL_BUTTON
                pszWindowTitle = pTitle,
                pszMainInstruction = pInstruction,
                pszContent = pContent,
                cButtons = 2,
                pButtons = buttons,
                nDefaultButton = BUTTON_OPEN_PROJECT,
            };

            var pressedButton = 0;
            var hr = TaskDialogIndirect(&config, &pressedButton, null, null);
            if (hr >= 0)
            {
                return pressedButton;
            }
        }

        var msgRes = MessageBoxW(0, "Open an existing .ghostproj project? (Yes = Open, No = Create New, Cancel = Exit)", "GhostEngine Studio", MB_YESNOCANCEL | MB_ICONQUESTION);
        return msgRes switch
        {
            IDYES => BUTTON_OPEN_PROJECT,
            IDNO => BUTTON_CREATE_PROJECT,
            _ => BUTTON_EXIT,
        };
    }

    private static string? ShowOpenProjectFileDialog(nint hwndOwner)
    {
        const int bufferLength = 1024;
        var fileBuffer = stackalloc char[bufferLength];
        fileBuffer[0] = '\0';

        fixed (char* pFilter = $"Ghost Project (*{Project.EXTENSION})\0*{Project.EXTENSION}\0All Files (*.*)\0*.*\0\0")
        fixed (char* pTitle = "Open GhostEngine Project")
        fixed (char* pDefExt = "ghostproj")
        {
            var ofn = new OPENFILENAMEW
            {
                lStructSize = (uint)sizeof(OPENFILENAMEW),
                hwndOwner = hwndOwner,
                lpstrFilter = pFilter,
                nFilterIndex = 1,
                lpstrFile = fileBuffer,
                nMaxFile = bufferLength,
                lpstrTitle = pTitle,
                lpstrDefExt = pDefExt,
                Flags = OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR,
            };

            return GetOpenFileNameW(&ofn) ? new string(fileBuffer) : null;
        }
    }

    private static string? ShowCreateProjectSaveDialog(nint hwndOwner)
    {
        const int bufferLength = 1024;
        var fileBuffer = stackalloc char[bufferLength];
        const string defaultName = "NewGhostProject.ghostproj";
        defaultName.AsSpan().CopyTo(new Span<char>(fileBuffer, bufferLength));
        fileBuffer[defaultName.Length] = '\0';

        fixed (char* pFilter = $"Ghost Project (*{Project.EXTENSION})\0*{Project.EXTENSION}\0\0")
        fixed (char* pTitle = "Create New GhostEngine Project")
        fixed (char* pDefExt = "ghostproj")
        {
            var ofn = new OPENFILENAMEW
            {
                lStructSize = (uint)sizeof(OPENFILENAMEW),
                hwndOwner = hwndOwner,
                lpstrFilter = pFilter,
                nFilterIndex = 1,
                lpstrFile = fileBuffer,
                nMaxFile = bufferLength,
                lpstrTitle = pTitle,
                lpstrDefExt = pDefExt,
                Flags = OFN_EXPLORER | OFN_PATHMUSTEXIST | OFN_OVERWRITEPROMPT | OFN_NOCHANGEDIR,
            };

            return GetSaveFileNameW(&ofn) ? new string(fileBuffer) : null;
        }
    }

    public static string? ShowOpenSceneFileDialog(string? initialDirectory = null, nint hwndOwner = 0)
    {
        const int bufferLength = 1024;
        var fileBuffer = stackalloc char[bufferLength];
        fileBuffer[0] = '\0';

        fixed (char* pFilter = "Ghost Scene (*.gscene)\0*.gscene\0All Files (*.*)\0*.*\0\0")
        fixed (char* pTitle = "Open Scene")
        fixed (char* pDefExt = "gscene")
        fixed (char* pInitDir = string.IsNullOrWhiteSpace(initialDirectory) ? null : initialDirectory)
        {
            var ofn = new OPENFILENAMEW
            {
                lStructSize = (uint)sizeof(OPENFILENAMEW),
                hwndOwner = hwndOwner,
                lpstrFilter = pFilter,
                nFilterIndex = 1,
                lpstrFile = fileBuffer,
                nMaxFile = bufferLength,
                lpstrInitialDir = pInitDir,
                lpstrTitle = pTitle,
                lpstrDefExt = pDefExt,
                Flags = OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR,
            };

            return GetOpenFileNameW(&ofn) ? new string(fileBuffer) : null;
        }
    }

    public static string? ShowSaveSceneFileDialog(string? initialDirectory = null, string defaultFileName = "NewScene.gscene", nint hwndOwner = 0)
    {
        const int bufferLength = 1024;
        var fileBuffer = stackalloc char[bufferLength];
        defaultFileName.AsSpan().CopyTo(new Span<char>(fileBuffer, bufferLength));
        fileBuffer[defaultFileName.Length] = '\0';

        fixed (char* pFilter = "Ghost Scene (*.gscene)\0*.gscene\0\0")
        fixed (char* pTitle = "Save Scene As")
        fixed (char* pDefExt = "gscene")
        fixed (char* pInitDir = string.IsNullOrWhiteSpace(initialDirectory) ? null : initialDirectory)
        {
            var ofn = new OPENFILENAMEW
            {
                lStructSize = (uint)sizeof(OPENFILENAMEW),
                hwndOwner = hwndOwner,
                lpstrFilter = pFilter,
                nFilterIndex = 1,
                lpstrFile = fileBuffer,
                nMaxFile = bufferLength,
                lpstrInitialDir = pInitDir,
                lpstrTitle = pTitle,
                lpstrDefExt = pDefExt,
                Flags = OFN_EXPLORER | OFN_PATHMUSTEXIST | OFN_OVERWRITEPROMPT | OFN_NOCHANGEDIR,
            };

            return GetSaveFileNameW(&ofn) ? new string(fileBuffer) : null;
        }
    }

    public static UnsavedPromptResult PromptSaveDirtyScene(string sceneDisplayName, nint hwndOwner = 0)
    {
        var res = MessageBoxW(hwndOwner, $"Save changes to '{sceneDisplayName}' before continuing?", "Unsaved Scene Changes", MB_YESNOCANCEL | MB_ICONQUESTION);
        return res switch
        {
            IDYES => UnsavedPromptResult.Save,
            IDNO => UnsavedPromptResult.Discard,
            _ => UnsavedPromptResult.Cancel,
        };
    }

    private static void ShowError(nint hwndOwner, string title, string message)
    {
        _ = MessageBoxW(hwndOwner, message, title, MB_OK | MB_ICONERROR);
    }
}
