using Ghost.Core;
using Ghost.Editor.View.Windows;

namespace Ghost.Editor.Core.AppState;

internal class LandingState : IAppState
{
    private LandingWindow? _window;

    public ValueTask<Result> OnExitingAsync()
    {
        if (App.Window == _window)
        {
            App.Window = null;
        }

        return ValueTask.FromResult(Result.Success());
    }

    public ValueTask<Result> OnEnteringAsync(object? parameter)
    {
        _window = App.GetService<LandingWindow>();
        _window.Activate();

        App.Window = _window;

        return ValueTask.FromResult(Result.Success());
    }

    public ValueTask<Result> OnExitedAsync()
    {
        _window?.Close();
        _window = null;

        return ValueTask.FromResult(Result.Success());
    }

    public ValueTask<Result> OnEnteredAsync(object? parameter)
    {
        return ValueTask.FromResult(Result.Success());
    }
}
