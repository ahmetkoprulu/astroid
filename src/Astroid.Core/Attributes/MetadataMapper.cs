using System.ComponentModel;
using System.Reflection;
using Newtonsoft.Json;

namespace Astroid.Core;

public class MetadataMapper
{
	public void MapProperties(object target, List<ProviderPropertyValue> values)
	{
		var type = target.GetType();
		var properties = type.GetProperties().Where(x => x.GetCustomAttribute<PropertyMetadataAttribute>() != null).ToList();

		foreach (var metadata in values)
		{
			var property = properties.SingleOrDefault(x => x.Name == metadata.Property);
			if (property == null || metadata.Value == null) continue;

			if (property.PropertyType == metadata.Value.GetType())
			{
				property.SetValue(target, metadata.Value);
				continue;
			}

			if (property.PropertyType.IsGenericType)
			{
				SetGenericValue(property, target, metadata);
				continue;
			}

			SetPrimitiveDynamically(property, target, metadata);
		}
	}

	public void SetGenericValue(PropertyInfo property, object target, ProviderPropertyValue pValue)
	{
		var valuestring = $"{pValue.Value}";
		if (!string.IsNullOrWhiteSpace(valuestring))
		{
			property.SetValue(target, JsonConvert.DeserializeObject(valuestring, property.PropertyType));
		}
	}

	public void SetPrimitiveDynamically(PropertyInfo property, object target, ProviderPropertyValue pValue)
	{
		var converter = TypeDescriptor.GetConverter(property.PropertyType);
		var vString = pValue.Value.ToString();
		if (string.IsNullOrEmpty(vString) && pValue.Required) throw new Exception($"Property {pValue.Property} is required.");

		var objValue = converter.ConvertFrom(vString!);
		property.SetValue(target, objValue);

		// var value = JsonConvert.DeserializeObject(propertyValue.Value.ToString(), property.PropertyType);
		// property.SetValue(provider, value);
	}
}
