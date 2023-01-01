using Astroid.Core;

namespace Astroid.Web.Models;

public class AMExchangeProvider
{
	public Guid Id { get; set; }
	public string Name { get; set; }
	public string Title { get; set; }
	public List<ProviderPropertyValue> Properties { get; set; }
}