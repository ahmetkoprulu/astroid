using Astroid.Core;

namespace Astroid.Web.Models;

public class AMProfile
{
	public Guid Id { get; set; }
	public string Name { get; set; }
	public string Email { get; set; }
	public string? Phone { get; set; }
	public string? TelegramId { get; set; }
	public ChannelType ChannelPreference { get; set; }
}
