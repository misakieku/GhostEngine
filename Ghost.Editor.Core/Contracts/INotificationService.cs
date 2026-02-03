using CommunityToolkit.WinUI.Behaviors;
using Ghost.Editor.Core.Notifications;

namespace Ghost.Editor.Core.Contracts;

public interface INotificationService
{
    public void ShowNotification(string? message, MessageType type, int duration = 5, string? title = null);
    public void ShowNotification(Notification notification);
}
