using Hexa.NET.ImGui;
using System.Numerics;

namespace Ghost.Studio.UI;

/// <summary>
/// Manages Editor panel visibility flags, INI layout persistence, and default Unity-style DockSpace arrangement.
/// </summary>
internal sealed unsafe class StudioDockspace
{
    private readonly string _iniPath;
    private bool _iniLoaded;
    private bool _layoutInitialized;
    private bool _resetRequested;

    public bool ShowImGuiDemo;
    public bool ShowImPlotDemo;

    public StudioDockspace()
    {
        _iniPath = Path.Combine(AppContext.BaseDirectory, "imgui.ini");
    }

    public void EnsureIniLoaded()
    {
        if (_iniLoaded)
        {
            return;
        }

        _iniLoaded = true;
        var io = ImGui.GetIO();
        io.IniFilename = null;

        if (File.Exists(_iniPath))
        {
            ImGui.LoadIniSettingsFromDisk(_iniPath);
            _layoutInitialized = true;
        }
    }

    public void RequestResetLayout()
    {
        _resetRequested = true;
        _layoutInitialized = false;
    }

    public void SaveLayout()
    {
        ImGui.SaveIniSettingsToDisk(_iniPath);
    }

    public void SyncAutoSaveIni()
    {
        var io = ImGui.GetIO();
        if (io.WantSaveIniSettings)
        {
            ImGui.SaveIniSettingsToDisk(_iniPath);
            io.WantSaveIniSettings = false;
        }
    }

    public void BeginDockSpace()
    {
        var viewport = ImGui.GetMainViewport();
        var dockspaceId = ImGui.GetID("GhostStudioDockSpace");

        if (!_layoutInitialized || _resetRequested)
        {
            _resetRequested = false;
            _layoutInitialized = true;
            BuildDefaultLayout(dockspaceId, viewport.WorkPos, viewport.WorkSize);
        }

        ImGui.DockSpaceOverViewport(dockspaceId, viewport, ImGuiDockNodeFlags.PassthruCentralNode);
    }

    private static void BuildDefaultLayout(uint dockspaceId, Vector2 workPos, Vector2 workSize)
    {
        if (workSize.X <= 1.0f || workSize.Y <= 1.0f)
        {
            return;
        }

        ImGuiP.DockBuilderRemoveNode(dockspaceId);
        ImGuiP.DockBuilderAddNode(dockspaceId, (ImGuiDockNodeFlags)(1 << 10) | ImGuiDockNodeFlags.PassthruCentralNode);
        ImGuiP.DockBuilderSetNodePos(dockspaceId, workPos);
        ImGuiP.DockBuilderSetNodeSize(dockspaceId, workSize);

        var mainId = dockspaceId;
        var leftId = 0u;
        var rightId = 0u;
        var bottomId = 0u;

        ImGuiP.DockBuilderSplitNode(mainId, ImGuiDir.Left, 0.19f, &leftId, &mainId);
        ImGuiP.DockBuilderSplitNode(mainId, ImGuiDir.Right, 0.27f, &rightId, &mainId);
        ImGuiP.DockBuilderSplitNode(mainId, ImGuiDir.Down, 0.29f, &bottomId, &mainId);

        ImGuiP.DockBuilderDockWindow("Hierarchy", leftId);
        ImGuiP.DockBuilderDockWindow("Inspector", rightId);
        ImGuiP.DockBuilderDockWindow("Scene", mainId);
        ImGuiP.DockBuilderDockWindow("Node Graph", mainId);
        ImGuiP.DockBuilderDockWindow("Project", bottomId);
        ImGuiP.DockBuilderDockWindow("Console", bottomId);
        ImGuiP.DockBuilderDockWindow("Profiler", bottomId);

        ImGuiP.DockBuilderFinish(dockspaceId);
    }
}
