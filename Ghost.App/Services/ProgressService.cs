using CommunityToolkit.WinUI;
using Ghost.Editor.Services.Contracts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Runtime.CompilerServices;

namespace Ghost.App.Services;

public class ProgressService : IProgressService
{
    private Grid? _progressBarContainer;
    private TextBlock? _progressMessage;
    private ProgressBar? _progressBar;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsInitialized()
    {
        return _progressBarContainer != null && _progressMessage != null && _progressBar != null;
    }

    internal void SetReference(Grid progressBarContainer)
    {
        _progressBarContainer = progressBarContainer;
        _progressMessage = _progressBarContainer.FindChild<TextBlock>();
        _progressBar = _progressBarContainer.FindChild<ProgressBar>();
    }

    public void ShowProgress(string message, double progress = 0.0)
    {
        if (!IsInitialized())
        {
            return;
        }

        _progressBarContainer!.Visibility = Visibility.Visible;
        _progressMessage!.Text = message;
        _progressBar!.Value = progress;
    }

    public void ShowIndeterminateProgress(string message)
    {
        if (!IsInitialized())
        {
            return;
        }

        _progressBarContainer!.Visibility = Visibility.Visible;
        _progressMessage!.Text = message;
        _progressBar!.IsIndeterminate = true;
    }

    public void SetProgress(double progress)
    {
        _progressBar!.Value = progress;
    }

    public void HideProgress()
    {
        if (!IsInitialized())
        {
            return;
        }

        _progressBarContainer!.Visibility = Visibility.Collapsed;
        _progressMessage!.Text = string.Empty;
        _progressBar!.Value = 0.0;
    }

    internal void ClearReference()
    {
        _progressBarContainer = null;
        _progressMessage = null;
        _progressBar = null;
    }
}