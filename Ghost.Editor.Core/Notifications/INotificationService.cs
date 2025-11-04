using CommunityToolkit.WinUI.Behaviors;

namespace Ghost.Editor.Core.Notifications;

public interface INotificationService
{
    public void ShowNotification(string? message, MessageType type, int duration = 5, string? title = null);
    public void ShowNotification(Notification notification);
}
