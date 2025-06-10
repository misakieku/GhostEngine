using Ghost.Editor.Models;

namespace Ghost.Editor.Services.Contracts;

public interface INotificationService
{
    public void ShowNotification(string? message, MessageType type, int duration = 5, string? title = null);
}
