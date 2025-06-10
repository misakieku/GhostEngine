using Ghost.App.View.Windows;
using Ghost.Data.Models;
using Ghost.Data.Services;
using Ghost.Editor;
using Ghost.Engine;
using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;

namespace Ghost.App.Infrastructures.AppState;

internal class EditorState : IAppState
{
    private EngineEditorWindow? _window;
    private EngineCore? _engineCore;

    public Task OnExitingAsync()
    {
        if (GhostApplication.Window == _window)
        {
            GhostApplication.Window = null;
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

        _engineCore = GhostApplication.GetService<EngineCore>();
        await _engineCore.StartAsync(new Engine.Models.LaunchArgument());

        _window = GhostApplication.GetService<EngineEditorWindow>();
        _window.Activate();

        GhostApplication.Window = _window;
    }

    public async Task OnExitedAsync()
    {
        if (_engineCore != null)
        {
            await _engineCore.ShutDownAsync();
        }

        if (GhostApplication.Window == _window)
        {
            GhostApplication.Window = null;
        }

        _window?.Close();
        _window = null;
    }

    public Task OnEnteredAsync(object? parameter)
    {
        EditorApplication.Activate(parameter, ((GhostApplication)(Application.Current)).Host.Services);
        return Task.CompletedTask;
    }
}