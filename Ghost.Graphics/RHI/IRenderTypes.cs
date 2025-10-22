namespace Ghost.Graphics.RHI;

/// <summary>
/// Root signature interface
/// </summary>
public interface IRootSignature : IDisposable
{
    /// <summary>
    /// Root signature name for debugging
    /// </summary>
    string Name
    {
        get; set;
    }
}