namespace Astroid.Core;

public class ServicesConfig
{
	public DatabaseConfig Database { get; set; } = new();
	public CacheConfig Cache { get; set; } = new();
	public MessageQueueConfig MessageQueue { get; set; } = new();
	public EmailConfig Email { get; set; } = new();
	public TelegramConfig Telegram { get; set; } = new();
}

public class EmailConfig
{
	public string Host { get; set; } = string.Empty;
	public int? Port { get; set; }
	public string UserName { get; set; } = string.Empty;
	public string Password { get; set; } = string.Empty;
}

public class TelegramConfig
{
	public string BotToken { get; set; } = string.Empty;
}

public class SmsConfig
{

}
