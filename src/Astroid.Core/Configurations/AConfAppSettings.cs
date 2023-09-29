namespace Astroid.Core;

public class AConfAppSettings
{
	public List<AConfEndpoint> Endpoints { get; set; } = new();
	public AConfFrontend Frontend { get; set; } = new();
	public AConfDatabase Database { get; set; } = new();
	public AConfCache Cache { get; set; } = new();
	public AConfMessageQueue MessageQueue { get; set; } = new();
}

public class AConfDatabase
{
	public string? ConnectionString { get; set; }
	public DatabaseProvider DatabaseProvider { get; set; }
}

public class AConfCache
{
	public string? ConnectionString { get; set; }
}
public class AConfMessageQueue
{
	public string? ConnectionString { get; set; }
}

public class AConfEndpoint
{
	public string? Url { get; set; }
	public ACCertificate? Certificate { get; set; }
}

public class AConfFrontend
{
	public string? HostName { get; set; }
	public int Port { get; set; }
	public bool Secure { get; set; }
}

public class ACCertificate
{
	public string? File { get; set; }
	public string? Password { get; set; }
	public string? Thumbprint { get; set; }
	public string? StoreName { get; set; }
	public string? StoreLocation { get; set; }
	public bool Validate { get; set; }
}
