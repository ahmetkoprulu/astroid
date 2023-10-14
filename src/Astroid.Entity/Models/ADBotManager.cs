using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Astroid.Core;

namespace Astroid.Entity;

[Table("BotManagers")]
public class ADBotManager : IEntity
{
	[Key]
	public Guid Id { get; set; }
	public Guid Key { get; set; }
	public DateTime CreatedDate { get; set; }
	public DateTime PingDate { get; set; }
}
