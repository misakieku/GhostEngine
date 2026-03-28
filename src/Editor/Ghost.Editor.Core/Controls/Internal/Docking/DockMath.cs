namespace Ghost.Editor.Core.Controls.Internal.Docking;

/// <summary>
/// Defines the possible dock positions for a drop operation.
/// </summary>
internal enum DockPosition
{
    Center,
    Top,
    Bottom,
    Left,
    Right,
    None
}

/// <summary>
/// Helper class for docking-related calculations.
/// </summary>
internal static class DockMath
{
    /// <summary>
    /// Calculates the dock position based on the relative position within a target element.
    /// Precedence: Left/Right win over Top/Bottom at corners.
    /// </summary>
    public static DockPosition CalculateDockPosition(double width, double height, double x, double y, double threshold)
    {
        // Guard against invalid inputs
        if (width <= 0 || height <= 0) return DockPosition.None;
        
        // Clamp threshold to valid range [0, 0.5]
        threshold = Math.Max(0, Math.Min(0.5, threshold));

        if (x < width * threshold) return DockPosition.Left;
        if (x > width * (1 - threshold)) return DockPosition.Right;
        if (y < height * threshold) return DockPosition.Top;
        if (y > height * (1 - threshold)) return DockPosition.Bottom;
        return DockPosition.Center;
    }
}
