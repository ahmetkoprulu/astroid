namespace Astroid.Core;

public class WebConfig
{
	public List<EndpointConfig> Endpoints { get; set; } = new();
	public FrontendConfig Frontend { get; set; } = new();
	public DatabaseConfig Database { get; set; } = new();
	public CacheConfig Cache { get; set; } = new();
	public MessageQueueConfig MessageQueue { get; set; } = new();
}

public class DatabaseConfig
{
	public string? ConnectionString { get; set; }
	public DatabaseProvider DatabaseProvider { get; set; }
}

public class CacheConfig
{
	public string? ConnectionString { get; set; }
}

public class MessageQueueConfig
{
	public string? ConnectionString { get; set; }
}

public class EndpointConfig
{
	public string? Url { get; set; }
	public CertificateConfig? Certificate { get; set; }
}

public class FrontendConfig
{
	public string? HostName { get; set; }
	public int Port { get; set; }
	public bool Secure { get; set; }
}

public class CertificateConfig
{
	public string? File { get; set; }
	public string? Password { get; set; }
	public string? Thumbprint { get; set; }
	public string? StoreName { get; set; }
	public string? StoreLocation { get; set; }
	public bool Validate { get; set; }
}
