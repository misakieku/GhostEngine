using Ghost.Core;
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

    public ValueTask<Result> OnExitingAsync()
    {
        if (App.Window == _window)
        {
            App.Window = null;
        }

        _engineCore?.Dispose();

        return ValueTask.FromResult(Result.Success());
    }

    public ValueTask<Result> OnEnteringAsync(object? parameter)
    {
        if (parameter is not ProjectMetadataInfo metadataInfo)
        {
            return ValueTask.FromResult(Result.Failure("Invalid parameter for entering EditorState."));
        }

        ProjectService.CurrentProject = metadataInfo;

        _engineCore = App.GetService<EngineCore>();
        _engineCore.Init();

        _window = App.GetService<EngineEditorWindow>();
        _window.Activate();

        App.Window = _window;

        return ValueTask.FromResult(Result.Success());
    }

    public ValueTask<Result> OnExitedAsync()
    {
        _window?.Close();
        _window = null;

        return ValueTask.FromResult(Result.Success());
    }

    public async ValueTask<Result> OnEnteredAsync(object? parameter)
    {
        await AssetDatabase.Initialize();
        return Result.Success();
    }
}