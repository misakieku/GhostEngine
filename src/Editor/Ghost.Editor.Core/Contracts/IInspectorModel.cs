using Microsoft.UI.Xaml;

namespace Ghost.Editor.Core.Contracts;

/// <summary>
/// Represents an active model for an object being inspected.
/// Responsible for generating its own UI.
/// </summary>
public interface IInspectorModel : IDisposable
{
    /// <summary>
    /// Generate the UI element that represents the body of the inspector for this model.
    /// </summary>
    UIElement BuildUI();
}

/// <summary>
/// An inspector model that requires continuous synchronization (e.g. per-frame updates).
/// </summary>
public interface ISyncableInspectorModel : IInspectorModel
{
    /// <summary>
    /// Called per-frame to sync data (e.g. from ECS to UI and back).
    /// </summary>
    void Sync();
}
