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
        if (EditorApplication.Window == _window)
        {
            EditorApplication.Window = null;
        }
        return Task.CompletedTask;
    }

    public async Task OnEnteringAsync(object? parameter)
    {
        if (parameter is not ProjectMetadataInfo metadataInfo)
        {
            throw new ArgumentException("Parameter must be of type ProjectMetadata.", nameof(parameter));
        }

        ProjectService.CurrentProject = metadataInfo;

        _engineCore = EditorApplication.GetService<EngineCore>();
        await _engineCore.StartAsync(new Engine.Models.LaunchArgument());

        _window = EditorApplication.GetService<EngineEditorWindow>();
        _window.Activate();

        EditorApplication.Window = _window;
    }

    public async Task OnExitedAsync()
    {
        if (_engineCore != null)
        {
            await _engineCore.ShutDownAsync();
        }

        if (EditorApplication.Window == _window)
        {
            EditorApplication.Window = null;
        }

        _window?.Close();
        _window = null;
    }

    public Task OnEnteredAsync(object? parameter)
    {
        AssetDatabase.Initialize();
        return Task.CompletedTask;
    }
}