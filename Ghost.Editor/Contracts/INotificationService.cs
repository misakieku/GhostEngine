using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Contracts;

internal interface INotificationService
{
    public void ShowNotification(string? message, InfoBarSeverity severity, int duration = 5, string? title = null);
}

internal interface INotificationService<T> : INotificationService
{
    public void Initialize(T notificationQueue);
    public void ClearQueueReference();
}