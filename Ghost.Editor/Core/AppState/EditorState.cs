using Ghost.Data.Models;
using Ghost.Data.Services;
using Ghost.Editor.Core.AssetHandle;
using Ghost.Editor.View.Windows;
using Ghost.Engine;
using Ghost.Engine.Services;
using Ghost.Graphics;
using Microsoft.UI.Xaml.Media;

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

        _engineCore?.ShutDown();
        CompositionTarget.Rendering -= OnRendering;

        return Task.CompletedTask;
    }

    public Task OnEnteringAsync(object? parameter)
    {
        if (parameter is not ProjectMetadataInfo metadataInfo)
        {
            throw new ArgumentException("Parameter must be of type ProjectMetadata.", nameof(parameter));
        }

        ProjectService.CurrentProject = metadataInfo;

        _engineCore = App.GetService<EngineCore>();
        _engineCore.Start(new Engine.Models.LaunchArgument());
        CompositionTarget.Rendering += OnRendering;

        _window = App.GetService<EngineEditorWindow>();
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
        AssetDatabase.Initialize();
        return Task.CompletedTask;
    }

    private void OnRendering(object? sender, object e)
    {
        if (GraphicsPipeline.IsGpuReady())
        {
            _window?.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, () =>
            {
                PlayerLoopService.Update();
                GraphicsPipeline.SignalCPUReady();
            });
        }
    }
}