namespace Astroid.Providers;

public class AMExchangeWallet
{
	public Guid Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string providerName { get; set; } = string.Empty;
	public bool IsHealthy { get; set; }
	public string? ErrorMessage { get; set; }
	public List<AMExchangeWalletAsset> Assets { get; set; } = new List<AMExchangeWalletAsset>();
	public decimal TotalBalance { get; set; }
	public decimal UnrealizedPnl { get; set; }
	public decimal RealizedPnl { get; set; }
	public decimal TotalPnl => UnrealizedPnl + RealizedPnl;
}

public class AMExchangeWalletAsset
{
	public string Name { get; set; } = string.Empty;
	public decimal Balance { get; set; }
}
