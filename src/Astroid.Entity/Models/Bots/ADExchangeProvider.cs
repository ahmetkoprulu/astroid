using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Astroid.Entity;

[Table("ExchangeProviders")]
public class ADExchangeProvider
{
	[Key]
	public Guid Id { get; set; }
	public string TargetType { get; set; }
	public string Title { get; set; }
	public string Name { get; set; }
}