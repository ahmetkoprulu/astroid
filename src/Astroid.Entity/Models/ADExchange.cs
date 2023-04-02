using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Astroid.Core;
using Newtonsoft.Json;

namespace Astroid.Entity;

[Table("Exchanges")]
public class ADExchange : IEntity
{
	[Key]
	public Guid Id { get; set; }
	public string Label { get; set; }
	public string? Description { get; set; }

	[Column(nameof(Properties))]
	public string PropertiesJson { get; set; }
	[NotMapped]
	public List<ProviderPropertyValue> Properties
	{
		get
		{
			if (string.IsNullOrEmpty(PropertiesJson)) return new List<ProviderPropertyValue>();

			return JsonConvert.DeserializeObject<List<ProviderPropertyValue>>(PropertiesJson)!;
		}
		set
		{
			PropertiesJson = JsonConvert.SerializeObject(value);
		}
	}

	public Guid ProviderId { get; set; }

	[ForeignKey(nameof(ProviderId))]
	public ADExchangeProvider Provider { get; set; }
	public Guid UserId { get; set; }
	public DateTime CreatedDate { get; set; }
	public DateTime ModifiedDate { get; set; }
}
