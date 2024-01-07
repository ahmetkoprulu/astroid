using Astroid.Core;
using Astroid.Entity;
using Astroid.Entity.Extentions;

namespace Astroid.Web;

public static class ExtensionMethods
{
	public static async Task SeedDatabase(this WebApplication app)
	{
		var scope = app.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<AstroidDb>();

		db.ExchangeProviders.Upsert(new ADExchangeProvider
		{
			Id = Guid.NewGuid(),
			Name = "binance-usd-futures",
			Title = "Binance USD Futures",
			TargetType = "Astroid.Providers.BinanceUsdFuturesProvider, Astroid.Providers",
			Type = ExchangeProviderType.UsdFutures
		}, x => x.Name == "binance-usd-futures");

		db.ExchangeProviders.Upsert(new ADExchangeProvider
		{
			Id = Guid.NewGuid(),
			Name = "binance-spot",
			Title = "Binance Spot",
			TargetType = "Astroid.Providers.BinanceSpotProvider, Astroid.Providers",
			Type = ExchangeProviderType.Spot
		}, x => x.Name == "binance-spot");

		await db.SaveChangesAsync();
	}
}
