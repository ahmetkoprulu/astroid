namespace Astroid.Core;

public class ProviderPropertyValue
{
	public string DisplayName { get; set; }
	public string? Description { get; set; }
	public bool IsNullable { get; set; }
	public object Value { get; set; }
	public object? DefaultValue { get; set; }
	public string? Property { get; set; }
	public PropertyTypes Type { get; set; } = PropertyTypes.UnTyped;
	public string? Group { get; set; }
	public bool Encrypted { get; set; }
	public bool Required { get; set; }
	public string? Data { get; set; }
	public int Order { get; set; }

	public ProviderPropertyValue() { }

	public ProviderPropertyValue(string property, object value) : this(string.Empty, property, value) { }

	public ProviderPropertyValue(string title, string property, object value, bool encrypted = false, bool required = false, PropertyTypes type = PropertyTypes.UnTyped, string data = default, string group = default)
	{
		DisplayName = title;
		Property = property;
		Value = value;
		Encrypted = encrypted;
		Required = required;
		Type = type;
		Data = data;
		Group = group;
	}

	public override string ToString() => $"{Value}";
}
