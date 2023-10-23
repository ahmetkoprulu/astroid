using Astroid.Core.Models;
using Microsoft.Extensions.Configuration;

namespace Astroid.Core.Notification
{
	public class Notificator
	{
		public INotificationChannel Channel { get; set; }
		public Dictionary<ChannelType, string> ChannelAssemblies { get; set; } = new() {
			{ ChannelType.Mail, "Astroid.Core.Notification.MailChannel, Astroid.Core"},
			{ ChannelType.Sms, "Astroid.Core.Notification.SmsChannel, Astroid.Core" },
			{ ChannelType.Telegram, "Astroid.Core.Notification.TelegramChannel, Astroid.Core" }
		};

		public Notificator(IConfiguration config, ChannelType channelType)
		{
			var isValid = ChannelAssemblies.TryGetValue(channelType, out var assembly);
			if (!isValid || assembly == null) throw new Exception("Invalid channel type");

			var type = Type.GetType(assembly) ?? throw new Exception("Invalid channel assembly");
			Channel = Activator.CreateInstance(type, config) as INotificationChannel ?? throw new Exception("Invalid channel instance");
		}

		public async Task<ServiceData> SendNotifications(string subject, string content, string to) => await Channel.Send(subject, content, to);
	}
}
