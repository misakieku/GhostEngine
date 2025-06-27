using Ghost.Data.Models;
using Ghost.Data.Services;
using Ghost.Editor.Core.AssetHandle;
using Ghost.Editor.View.Windows;
using Ghost.Engine;

namespace Ghost.Editor.Core.AppState;

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

    public Task OnEnteringAsync(object? parameter)
    {
        if (parameter is not ProjectMetadataInfo metadataInfo)
        {
            throw new ArgumentException("Parameter must be of type ProjectMetadata.", nameof(parameter));
        }

        ProjectService.CurrentProject = metadataInfo;

        _window = App.GetService<EngineEditorWindow>();
        _window.Activate();

        App.Window = _window;

        return Task.CompletedTask;
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

    public async Task OnEnteredAsync(object? parameter)
    {
        AssetDatabase.Initialize();

        _engineCore = App.GetService<EngineCore>();
        await _engineCore.StartAsync(new Engine.Models.LaunchArgument());
    }
}