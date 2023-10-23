using System.ComponentModel;
using Astroid.Core;
using Newtonsoft.Json;

namespace Astroid.Entity.Extentions;

public static class ContextExtentionMethods
{
	public static string GetDescription<T>(this T value) where T : Enum
	{
		var fieldInfo = value.GetType().GetField(value.ToString());
		var descriptionAttributes = fieldInfo?.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];
		if (descriptionAttributes?.Any() ?? false) return descriptionAttributes.First().Description;

		return value.ToString();
	}
}
