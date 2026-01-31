using Ghost.Core;

namespace Ghost.Editor.Core.AppState;

internal interface IAppState
{
    /// <summary>
    /// Called when exiting the state.
    /// </summary>
    public ValueTask<Result> OnExitingAsync();

    /// <summary>
    /// Called when entering the state, right after OnEnteringAsync.
    /// <paramref name="parameter">can be used to pass data into the state, such as a project to load.</summary>
    /// </summary>
    public ValueTask<Result> OnEnteringAsync(object? parameter);

    /// <summary>
    /// Called when exiting the state, specifically for pose transitions.
    /// </summary>
    public ValueTask<Result> OnExitedAsync();

    /// <summary>
    /// Called when entered the state, specifically after the state has been fully initialized and is ready for interaction.
    /// </summary>
    /// <param name="parameter">can be used to pass data into the state, such as a project to load.</param>
    public ValueTask<Result> OnEnteredAsync(object? parameter);
}