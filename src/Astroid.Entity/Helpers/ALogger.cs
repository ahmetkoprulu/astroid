using Microsoft.Extensions.Logging;

namespace Astroid.Log;

public static class ALogger
{
	public static ILoggerFactory Factory { get; set; }

	public static ILogger<T> Create<T>()
	{
		try
		{
			return Factory.CreateLogger<T>();
		}
		catch {/* ignored */}
		return null;
	}

	public static ILogger CreateLogger(string categoryName)
	{
		try
		{
			return Factory.CreateLogger(categoryName);
		}
		catch {/* ignored */}
		return null;
	}
}

