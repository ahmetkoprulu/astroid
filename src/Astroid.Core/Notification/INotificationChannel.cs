using Astroid.Core.Models;

namespace Astroid.Core.Notification;

public interface INotificationChannel
{
	ServiceData Send(string subject, string content, string to, params NotificationAttachment[] attachments);
}