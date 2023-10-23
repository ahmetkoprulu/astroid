using Astroid.Core.Models;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;

namespace Astroid.Core.Notification;

public class TelegramChannel : INotificationChannel
{
	private readonly string Token = string.Empty;

	public TelegramChannel(IConfiguration config)
	{
		var settings = config.Get<ServicesConfig>() ?? new();
		Token = settings.Telegram.BotToken ?? Environment.GetEnvironmentVariable("ASTROID_TELEGRAM_BOT_TOKEN") ?? throw new Exception("ASTROID_TELEGRAM_BOT_TOKEN");
	}

	public async Task<ServiceData> Send(string subject, string content, string to, params NotificationAttachment[] attachments)
	{
		var message = $"{subject}\n{content}";
		try
		{
			var client = new TelegramBotClient(Token);
			await client.SendTextMessageAsync(to, message);

			return new ServiceData
			{
				Success = true,
				Message = "Message sent successfully."
			};
		}
		catch (Exception ex)
		{
			return new ServiceData
			{
				Success = false,
				Message = $"Error while sending mail.\nError: {ex.Message} \nMessage: {ex.StackTrace}"
			};
		}
	}
}
