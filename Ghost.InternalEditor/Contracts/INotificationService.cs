using Microsoft.UI.Xaml.Controls;

namespace Ghost.App.Contracts;

internal interface INotificationService
{
    public void ShowNotification(string? message, InfoBarSeverity severity, int duration = 5, string? title = null);
}

internal interface INotificationService<T> : INotificationService
{
    public void Initialize(T notificationQueue);
    public void ClearQueueReference();
}