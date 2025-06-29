using Ghost.Editor.View.Windows;

namespace Ghost.Editor.Core.AppState;

internal class LandingState : IAppState
{
    private LandingWindow? _window;

    public Task OnExitingAsync()
    {
        if (App.Window == _window)
        {
            App.Window = null;
        }

        return Task.CompletedTask;
    }

    public Task OnEnteringAsync(object? parameter)
    {
        _window = App.GetService<LandingWindow>();
        _window.Activate();

        App.Window = _window;

        return Task.CompletedTask;
    }

    public Task OnExitedAsync()
    {
        _window?.Close();
        _window = null;

        return Task.CompletedTask;
    }

    public Task OnEnteredAsync(object? parameter)
    {
        return Task.CompletedTask;
    }
}
