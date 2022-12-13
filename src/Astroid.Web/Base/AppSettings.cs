namespace Astroid.Web;

public class ACAppSettings
{
	public List<ACEndpoint> Endpoints { get; set; }
}

public class ACEndpoint
{
	public string Url { get; set; }
	public ACCertificate Certificate { get; set; }
}

public class ACCertificate
{
	public string File { get; set; }
	public string Password { get; set; }
	public string Thumbprint { get; set; }
	public string StoreName { get; set; }
	public string StoreLocation { get; set; }
	public bool Validate { get; set; }
}
