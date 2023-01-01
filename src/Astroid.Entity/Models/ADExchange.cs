using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Astroid.Core;
using System.Text.Json;

namespace Astroid.Entity;

[Table("Exchanges")]
public class ADExchange : IEntity
{
	[Key]
	public Guid Id { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }

	[Column(nameof(Properties))]
	public string PropertiesJson { get; set; }
	[NotMapped]
	public List<ProviderPropertyValue> Properties
	{
		get
		{
			if (string.IsNullOrEmpty(PropertiesJson)) return new List<ProviderPropertyValue>();

			return JsonSerializer.Deserialize<List<ProviderPropertyValue>>(PropertiesJson)!;
		}
		set
		{
			PropertiesJson = JsonSerializer.Serialize(value);
		}
	}

	public Guid ProviderId { get; set; }

	[ForeignKey(nameof(ProviderId))]
	public ADExchangeProvider Provider { get; set; }
	public Guid UserId { get; set; }
	public DateTime CreatedDate { get; set; }
	public DateTime ModifiedDate { get; set; }
}
