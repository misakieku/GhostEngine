using Ghost.Data.Models;
using Ghost.Data.Services;
using Ghost.Editor.View.Windows;
using Ghost.Engine;
using System.Threading.Tasks;

namespace Ghost.Editor.Infrastructures.AppState;

internal class EditorState : IAppState
{
    private EngineEditorWindow? _window;
    private EngineCore? _engineCore;

    public Task OnExitingAsync()
    {
        if (App.Window == _window)
        {
            App.Window = null;
        }
        return Task.CompletedTask;
    }

    public async Task OnEnteringAsync(object? parameter)
    {
        if (parameter is not ProjectMetadataInfo metadataInfo)
        {
            throw new System.ArgumentException("Parameter must be of type ProjectMetadata.", nameof(parameter));
        }

        ProjectService.CurrentProject = metadataInfo;

        _engineCore = App.GetService<EngineCore>();
        await _engineCore.StartAsync(new Engine.Models.LaunchArgument());

        _window = App.GetService<EngineEditorWindow>();
        _window.Activate();

        App.Window = _window;
    }

    public async Task OnExitedAsync()
    {
        if (_engineCore != null)
        {
            await _engineCore.ShutDownAsync();
        }

        if (App.Window == _window)
        {
            App.Window = null;
        }

        _window?.Close();
        _window = null;
    }

    public Task OnEnteredAsync(object? parameter)
    {
        return Task.CompletedTask;
    }
}