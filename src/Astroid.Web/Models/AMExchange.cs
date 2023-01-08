using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Astroid.Core;

namespace Astroid.Web.Models;

public class AMExchange
{
	public Guid Id { get; set; }
	public string Name { get; set; }
	public string? Description { get; set; }
	public List<ProviderPropertyValue> Properties { get; set; }
	public Guid ProviderId { get; set; }
	public string? ProviderName { get; set; }
}