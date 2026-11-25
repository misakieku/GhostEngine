using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using Hexa.NET.ImNodes;
using Hexa.NET.ImPlot;

namespace Ghost.Studio.Backend;

internal sealed unsafe class ImGuiContextManager : IDisposable
{
    public const uint IMGUI_MANAGED_TEX_BIT = 0x8000_0000u;

    // Nerd Font Private Use Area ranges (BMP icons + Material Design SMP icons) pinned for ImFontAtlas lifetime.
    private static readonly uint[] s_nerdIconRanges = GC.AllocateArray<uint>(5, pinned: true);

    static ImGuiContextManager()
    {
        s_nerdIconRanges[0] = 0xE000u;
        s_nerdIconRanges[1] = 0xF8FFu;
        s_nerdIconRanges[2] = 0xF0000u;
        s_nerdIconRanges[3] = 0xF2000u;
        s_nerdIconRanges[4] = 0u;
    }

    private readonly ImGuiContextPtr _imguiContext;
    private readonly ImPlotContextPtr _implotContext;
    private readonly ImNodesContextPtr _imnodesContext;
    private ImGuiStyle _baseStyle;
    private EditorThemePreset _themePreset = EditorThemePreset.Dark;
    private EditorColorTokens _tokens;
    private float _dpiScale = 1.0f;
    private bool _disposed;

    public float DpiScale => _dpiScale;
    public EditorThemePreset ThemePreset => _themePreset;
    public ref readonly EditorColorTokens Tokens => ref _tokens;
    public ImFontPtr RegularFont { get; private set; }
    public ImFontPtr HeadingFont { get; private set; }
    public ImFontPtr MonospaceFont { get; private set; }

    public ImGuiContextManager()
    {
        _imguiContext = ImGui.CreateContext();
        ImGui.SetCurrentContext(_imguiContext);

        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.DockingEnable;
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset | ImGuiBackendFlags.RendererHasTextures;

        ImGuizmo.SetImGuiContext(_imguiContext);

        _implotContext = ImPlot.CreateContext();
        ImPlot.SetCurrentContext(_implotContext);
        ImPlot.SetImGuiContext(_imguiContext);

        _imnodesContext = ImNodes.CreateContext();
        ImNodes.SetCurrentContext(_imnodesContext);
        ImNodes.SetImGuiContext(_imguiContext);

        ApplyThemeInternal(_themePreset);
        ConfigureFonts(io);
    }

    public void MakeCurrent()
    {
        ImGui.SetCurrentContext(_imguiContext);
        ImPlot.SetCurrentContext(_implotContext);
        ImNodes.SetCurrentContext(_imnodesContext);
    }

    public void SetTheme(EditorThemePreset preset)
    {
        if (_themePreset == preset)
        {
            return;
        }

        _themePreset = preset;
        MakeCurrent();
        ApplyThemeInternal(preset);
        ApplyDpiToCurrentStyle();
    }

    public void SetDpiScale(float dpiScale)
    {
        var clamped = Math.Clamp(dpiScale, 0.5f, 4.0f);
        if (MathF.Abs(clamped - _dpiScale) < 0.01f)
        {
            return;
        }

        _dpiScale = clamped;
        MakeCurrent();
        ApplyDpiToCurrentStyle();
    }

    private void ApplyThemeInternal(EditorThemePreset preset)
    {
        var style = ImGui.GetStyle();
        var plotStyle = ImPlot.GetStyle();
        EditorTheme.Apply(style, plotStyle, preset);
        _tokens = EditorTheme.GetTokens(preset);
        _baseStyle = *(ImGuiStyle*)style;
    }

    private void ApplyDpiToCurrentStyle()
    {
        var style = ImGui.GetStyle();
        *(ImGuiStyle*)style = _baseStyle;
        style.ScaleAllSizes(_dpiScale);
        style.FontScaleDpi = _dpiScale;
    }

    private void ConfigureFonts(ImGuiIOPtr io)
    {
        io.Fonts.TexDesiredFormat = ImTextureFormat.Rgba32;

        var fontDir = Path.Combine(AppContext.BaseDirectory, "Resources", "Fonts");
        var nerdFontPath = Path.Combine(fontDir, "JetBrainsMonoNerdFontMono-Regular.ttf");

        var regularPath = ResolveFirstExistingFont(Path.Combine(fontDir, "Inter-Regular.ttf"), @"C:\Windows\Fonts\segoeui.ttf");
        RegularFont = regularPath != null ? io.Fonts.AddFontFromFileTTF(regularPath, EditorTheme.FONT_SIZE_REGULAR) : io.Fonts.AddFontDefault();
        MergeNerdIcons(io, nerdFontPath, EditorTheme.FONT_SIZE_REGULAR * 1.5f);

        var headingPath = ResolveFirstExistingFont(Path.Combine(fontDir, "Inter-SemiBold.ttf"), @"C:\Windows\Fonts\segoeuib.ttf");
        HeadingFont = headingPath != null ? io.Fonts.AddFontFromFileTTF(headingPath, EditorTheme.FONT_SIZE_HEADING) : RegularFont;
        if (headingPath != null)
        {
            MergeNerdIcons(io, nerdFontPath, EditorTheme.FONT_SIZE_HEADING * 1.5f);
        }

        var monoPath = ResolveFirstExistingFont(nerdFontPath, @"C:\Windows\Fonts\consola.ttf");
        MonospaceFont = monoPath != null ? io.Fonts.AddFontFromFileTTF(monoPath, EditorTheme.FONT_SIZE_MONO) : RegularFont;

        io.FontDefault = RegularFont;
    }

    private static void MergeNerdIcons(ImGuiIOPtr io, string nerdFontPath, float sizePixels)
    {
        if (!File.Exists(nerdFontPath))
        {
            return;
        }

        var mergeConfig = ImGui.ImFontConfig();
        mergeConfig.MergeMode = true;
        mergeConfig.PixelSnapH = true;

        fixed (uint* pRanges = s_nerdIconRanges)
        {
            io.Fonts.AddFontFromFileTTF(nerdFontPath, sizePixels, mergeConfig, pRanges);
        }

        mergeConfig.Destroy();
    }

    private static string? ResolveFirstExistingFont(string primaryPath, string fallbackPath)
    {
        if (File.Exists(primaryPath))
        {
            return primaryPath;
        }

        return File.Exists(fallbackPath) ? fallbackPath : null;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        ImNodes.DestroyContext(_imnodesContext);
        ImPlot.DestroyContext(_implotContext);

        // Clear external DLL GImGui pointers before freeing _imguiContext so CRT atexit destructors (e.g. ImGuizmo's static Context) do not dereference a freed ImGuiContext.
        ImGuizmo.SetImGuiContext(default(ImGuiContextPtr));
        ImNodes.SetImGuiContext(default(ImGuiContextPtr));
        ImPlot.SetImGuiContext(default(ImGuiContextPtr));

        ImGui.DestroyContext(_imguiContext);
    }
}
