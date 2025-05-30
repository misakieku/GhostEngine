using Ghost.Editor.Contracts;
using Ghost.Editor.View.Windows;
using System.Threading.Tasks;

namespace Ghost.Editor.AppStates;

internal class LandingState : IAppState<StateKey>
{
    private LandingWindow? _window;

    public StateKey StateKy => StateKey.Landing;

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
        App.Window = _window;

        _window.Activate();
        return Task.CompletedTask;
    }

    public Task OnExitedAsync()
    {
        if (App.Window == _window)
        {
            App.Window = null;
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
