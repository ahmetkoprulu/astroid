using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Astroid.Core;

namespace Astroid.Entity;

[Table("Notifications")]
public class ADNotification
{
	[Key]
	public Guid Id { get; set; }
	public Guid UserId { get; set; }
	public DateTime CreatedDate { get; set; }
	public DateTime SentDate { get; set; }
	public DateTime ExpireDate { get; set; }
	public ChannelType Channel { get; set; }
	public NotificationStatus Status { get; set; }
	public string Subject { get; set; }
	public string Content { get; set; }
	public string To { get; set; }
	public string? Error { get; set; }

	public ADUser User { get; set; }

	[NotMapped]
	public bool IsExpired => ExpireDate != DateTime.MinValue && ExpireDate < DateTime.UtcNow;
}
