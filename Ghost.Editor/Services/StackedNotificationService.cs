using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Ghost.Editor.Services;

public class StackedNotificationService
{
    private InfoBar? _infoBar;
    private StackedNotificationsBehavior? _notificationQueue;

    internal void SetReference(InfoBar infoBar, StackedNotificationsBehavior notificationQueue)
    {
        _infoBar = infoBar;
        _notificationQueue = notificationQueue;
    }

    internal void ClearReference()
    {
        if (_infoBar != null)
        {
            _infoBar.IsOpen = false;
        }
        _infoBar = null;
        _notificationQueue = null;
    }

    public void ShowNotification(string? message, InfoBarSeverity severity, int duration = 5, string? title = null)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var notification = new Notification
        {
            Message = message,
            Severity = severity,
            Duration = TimeSpan.FromSeconds(duration),
            Title = title
        };

        ShowNotification(notification);
    }

    public void ShowNotification(Notification notification)
    {
        _notificationQueue?.Show(notification);
    }
}