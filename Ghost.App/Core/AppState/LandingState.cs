using Ghost.Editor;
using Ghost.Editor.View.Windows;
using System.Threading.Tasks;

namespace Ghost.Editor.Core.AppState;

internal class LandingState : IAppState
{
    private LandingWindow? _window;

    public Task OnExitingAsync()
    {
        if (EditorApplication.Window == _window)
        {
            EditorApplication.Window = null;
        }
        return Task.CompletedTask;
    }

    public Task OnEnteringAsync(object? parameter)
    {
        _window = EditorApplication.GetService<LandingWindow>();
        EditorApplication.Window = _window;

        _window.Activate();
        return Task.CompletedTask;
    }

    public Task OnExitedAsync()
    {
        if (EditorApplication.Window == _window)
        {
            EditorApplication.Window = null;
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
