namespace Astroid.Core;

[AttributeUsage(AttributeTargets.Property)]
public class PropertyMetadataAttribute : Attribute
{
	public string? DisplayName { get; set; }
	public string? Description { get; set; }
	public PropertyTypes Type { get; set; }
	public string? Group { get; set; }
	public bool Required { get; set; }
	public bool IsEncrypted { get; set; }
	public string? Data { get; set; }
	public object DefaultValue { get; set; }
	public int Order { get; set; }

	public PropertyMetadataAttribute(string? title = default, string? description = default, bool isRequired = false, bool isEncrypted = false, int sort = 0, PropertyTypes type = PropertyTypes.Text, string? data = default, string? group = default)
	{
		IsEncrypted = isEncrypted;
		DisplayName = title;
		Description = description;
		Order = sort;
		Required = isRequired;
		Type = type;
		Data = data;
		Group = group;
	}
}

