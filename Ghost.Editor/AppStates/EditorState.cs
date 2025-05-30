using Ghost.Data.Models;
using Ghost.Editor.Contracts;
using Ghost.Editor.View.Windows;
using System.Threading.Tasks;

namespace Ghost.Editor.AppStates;

internal class EditorState : IAppState<StateKey>
{
    private EngineEditorWindow? _window;

    public StateKey StateKy => StateKey.EngineEditor;

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
        if (parameter is not ProjectMetadata metadata)
        {
            throw new System.ArgumentException("Parameter must be of type ProjectMetadata.", nameof(parameter));
        }

        _window = App.GetService<EngineEditorWindow>();
        _window.ViewModel.CurrentProject = metadata;
        _window.Activate();

        App.Window = _window;
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