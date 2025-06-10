using Ghost.App.View.Windows;
using System.Threading.Tasks;

namespace Ghost.App.Infrastructures.AppState;

internal class LandingState : IAppState
{
    private LandingWindow? _window;

    public Task OnExitingAsync()
    {
        if (GhostApplication.Window == _window)
        {
            GhostApplication.Window = null;
        }
        return Task.CompletedTask;
    }

    public Task OnEnteringAsync(object? parameter)
    {
        _window = GhostApplication.GetService<LandingWindow>();
        GhostApplication.Window = _window;

        _window.Activate();
        return Task.CompletedTask;
    }

    public Task OnExitedAsync()
    {
        if (GhostApplication.Window == _window)
        {
            GhostApplication.Window = null;
        }

        _window?.Close();
        _window = null;
        return Task.CompletedTask;
    }

    public Task OnEnteredAsync(object? parameter)
    {
        return Task.CompletedTask;
    }
}
