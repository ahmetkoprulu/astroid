namespace Astroid.Core;

public class BotManagerConfig
{
	public Guid Id { get; set; }
	public DatabaseConfig Database { get; set; } = new();
	public CacheConfig Cache { get; set; } = new();
	public MessageQueueConfig MessageQueue { get; set; } = new();
}
