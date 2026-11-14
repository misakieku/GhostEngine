using System.Text.Json;

namespace Ghost.Studio.Workspace;

/// <summary>
/// Serializable size and active-tab configuration for the studio workspace.
/// </summary>
public sealed record WorkspaceLayoutState
{
    public const double DEFAULT_LEFT_WIDTH = 260;
    public const double DEFAULT_RIGHT_WIDTH = 340;
    public const double DEFAULT_BOTTOM_HEIGHT = 240;

    public const double MIN_SIDE_WIDTH = 180;
    public const double MAX_SIDE_WIDTH = 600;
    public const double MIN_BOTTOM_HEIGHT = 120;
    public const double MAX_BOTTOM_HEIGHT = 540;

    public double LeftWidth { get; init; } = DEFAULT_LEFT_WIDTH;
    public double RightWidth { get; init; } = DEFAULT_RIGHT_WIDTH;
    public double BottomHeight { get; init; } = DEFAULT_BOTTOM_HEIGHT;
    public string BottomActiveTabKey { get; init; } = "Project";

    /// <summary>
    /// Returns a normalized copy with pane dimensions clamped to usable bounds.
    /// </summary>
    public WorkspaceLayoutState Clamped()
    {
        return this with
        {
            LeftWidth = ClampFinite(LeftWidth, MIN_SIDE_WIDTH, MAX_SIDE_WIDTH, DEFAULT_LEFT_WIDTH),
            RightWidth = ClampFinite(RightWidth, MIN_SIDE_WIDTH, MAX_SIDE_WIDTH, DEFAULT_RIGHT_WIDTH),
            BottomHeight = ClampFinite(BottomHeight, MIN_BOTTOM_HEIGHT, MAX_BOTTOM_HEIGHT, DEFAULT_BOTTOM_HEIGHT),
            BottomActiveTabKey = string.IsNullOrEmpty(BottomActiveTabKey) ? "Project" : BottomActiveTabKey
        };
    }

    /// <summary>
    /// Loads persisted workspace state or returns defaults if none exists or parsing fails.
    /// </summary>
    public static WorkspaceLayoutState Load()
    {
        try
        {
            var path = GetSettingsFilePath();
            if (!File.Exists(path))
            {
                return new WorkspaceLayoutState();
            }

            var json = File.ReadAllText(path);
            var parsed = JsonSerializer.Deserialize<WorkspaceLayoutState>(json);
            return parsed?.Clamped() ?? new WorkspaceLayoutState();
        }
        catch
        {
            return new WorkspaceLayoutState();
        }
    }

    /// <summary>
    /// Persists the current workspace state to disk, ignoring non-fatal IO failures.
    /// </summary>
    public void Save()
    {
        try
        {
            var path = GetSettingsFilePath();
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(Clamped());
            File.WriteAllText(path, json);
        }
        catch
        {
            // Layout persistence is best-effort and must never crash the studio.
        }
    }

    private static double ClampFinite(double value, double min, double max, double fallback)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return fallback;
        }

        return Math.Clamp(value, min, max);
    }

    private static string GetSettingsFilePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "GhostEngine", "Studio", "workspace_layout.json");
    }
}
