namespace Astroid.Core;

[AttributeUsage(AttributeTargets.Property)]
public class PropertyMetadataAttribute : Attribute
{
	public string? DisplayName { get; set; }
	public string? Description { get; set; }
	public PropertyTypes Type { get; set; }
	public string? Group { get; set; }
	public bool Required { get; set; }
	public bool Encrypted { get; set; }
	public string? Data { get; set; }
	public object DefaultValue { get; set; }
	public int Order { get; set; }

	public PropertyMetadataAttribute(string? title = default, string? description = default, bool required = false, bool encrypted = false, int sort = 0, PropertyTypes type = PropertyTypes.Text, string? data = default, string? group = default)
	{
		Encrypted = encrypted;
		DisplayName = title;
		Description = description;
		Order = sort;
		Required = required;
		Type = type;
		Data = data;
		Group = group;
	}
}

