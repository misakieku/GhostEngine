using Ghost.Studio.Backend;
using Hexa.NET.ImGui;
using Hexa.NET.ImNodes;
using System.Numerics;

namespace Ghost.Studio.UI;

/// <summary>
/// Docked interactive Render/Material node graph canvas powered by <see cref="ImNodes"/>.
/// </summary>
internal sealed unsafe class NodeGraphPanel
{
    private readonly struct LinkEntry
    {
        public readonly int Id;
        public readonly int StartAttr;
        public readonly int EndAttr;

        public LinkEntry(int id, int startAttr, int endAttr)
        {
            Id = id;
            StartAttr = startAttr;
            EndAttr = endAttr;
        }
    }

    private readonly List<LinkEntry> _links = new();
    private int _nextLinkId = 100;
    private bool _positionsInitialized;

    public NodeGraphPanel()
    {
        _links.Add(new LinkEntry(1, 102, 201));
        _links.Add(new LinkEntry(2, 202, 301));
    }

    public void Draw(ImGuiContextManager contextManager, ref bool isOpen)
    {
        ImGui.SetNextWindowPos(new Vector2(300.0f, 32.0f), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(960.0f, 600.0f), ImGuiCond.FirstUseEver);

        if (!ImGui.Begin("Node Graph", ref isOpen))
        {
            ImGui.End();
            return;
        }

        ref readonly var tokens = ref contextManager.Tokens;
        ImGui.TextColored(tokens.TextSecondary, "Render / Shader Graph Canvas  |  Drag pins to link, Alt+Click link to disconnect");
        ImGui.Separator();

        ImNodes.BeginNodeEditor();

        if (!_positionsInitialized)
        {
            ImNodes.SetNodeGridSpacePos(1, new Vector2(60.0f, 90.0f));
            ImNodes.SetNodeGridSpacePos(2, new Vector2(320.0f, 90.0f));
            ImNodes.SetNodeGridSpacePos(3, new Vector2(590.0f, 110.0f));
            _positionsInitialized = true;
        }

        DrawSceneColorNode();
        DrawToneMapNode();
        DrawFinalOutputNode();

        for (var i = 0; i < _links.Count; i++)
        {
            var link = _links[i];
            ImNodes.Link(link.Id, link.StartAttr, link.EndAttr);
        }

        ImNodes.MiniMap(0.18f, ImNodesMiniMapLocation.BottomRight);
        ImNodes.EndNodeEditor();

        var startAttr = 0;
        var endAttr = 0;
        if (ImNodes.IsLinkCreated(ref startAttr, ref endAttr))
        {
            _links.Add(new LinkEntry(_nextLinkId++, startAttr, endAttr));
        }

        var destroyedLinkId = 0;
        if (ImNodes.IsLinkDestroyed(ref destroyedLinkId))
        {
            RemoveLink(destroyedLinkId);
        }

        if (ImGui.IsKeyPressed(ImGuiKey.Delete))
        {
            var numSelected = ImNodes.NumSelectedLinks();
            if (numSelected > 0)
            {
                var selected = stackalloc int[Math.Min(numSelected, 64)];
                ImNodes.GetSelectedLinks(selected);
                for (var i = 0; i < Math.Min(numSelected, 64); i++)
                {
                    RemoveLink(selected[i]);
                }
            }
        }

        ImGui.End();
    }

    private static void DrawSceneColorNode()
    {
        ImNodes.BeginNode(1);
        ImNodes.BeginNodeTitleBar();
        ImGui.TextUnformatted("Scene Color RT");
        ImNodes.EndNodeTitleBar();

        ImNodes.BeginOutputAttribute(102);
        ImGui.TextUnformatted("HDR Color");
        ImNodes.EndOutputAttribute();
        ImNodes.EndNode();
    }

    private static void DrawToneMapNode()
    {
        ImNodes.BeginNode(2);
        ImNodes.BeginNodeTitleBar();
        ImGui.TextUnformatted("ACESToneMap");
        ImNodes.EndNodeTitleBar();

        ImNodes.BeginInputAttribute(201);
        ImGui.TextUnformatted("In Color");
        ImNodes.EndInputAttribute();

        ImNodes.BeginOutputAttribute(202);
        ImGui.TextUnformatted("LDR Out");
        ImNodes.EndOutputAttribute();
        ImNodes.EndNode();
    }

    private static void DrawFinalOutputNode()
    {
        ImNodes.BeginNode(3);
        ImNodes.BeginNodeTitleBar();
        ImGui.TextUnformatted("Viewport Target");
        ImNodes.EndNodeTitleBar();

        ImNodes.BeginInputAttribute(301);
        ImGui.TextUnformatted("Result");
        ImNodes.EndInputAttribute();
        ImNodes.EndNode();
    }

    private void RemoveLink(int linkId)
    {
        for (var i = _links.Count - 1; i >= 0; i--)
        {
            if (_links[i].Id == linkId)
            {
                _links.RemoveAt(i);
            }
        }
    }
}
