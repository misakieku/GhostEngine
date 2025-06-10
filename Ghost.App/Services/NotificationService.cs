using CommunityToolkit.WinUI.Behaviors;
using Ghost.Editor.Models;
using Ghost.Editor.Services.Contracts;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Ghost.App.Services;

public class NotificationService : INotificationService
{
    private InfoBar? _infoBar;
    private StackedNotificationsBehavior? _notificationQueue;

    internal void SetReference(InfoBar infoBar, StackedNotificationsBehavior notificationQueue)
    {
        _infoBar = infoBar;
        _notificationQueue = notificationQueue;
    }

    public void ShowNotification(string? message, MessageType type, int duration = 5, string? title = null)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var notification = new Notification
        {
            Message = message,
            Severity = (InfoBarSeverity)type,
            Duration = TimeSpan.FromSeconds(duration),
            Title = title
        };

        ShowNotification(notification);
    }

    public void ShowNotification(Notification notification)
    {
        _notificationQueue?.Show(notification);
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
}