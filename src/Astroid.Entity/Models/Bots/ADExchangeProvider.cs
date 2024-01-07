using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Astroid.Core;

namespace Astroid.Entity;

[Table("ExchangeProviders")]
public class ADExchangeProvider
{
	[Key]
	public Guid Id { get; set; }
	public string TargetType { get; set; }
	public string Title { get; set; }
	public string Name { get; set; }
	public ExchangeProviderType Type { get; set; }
}
