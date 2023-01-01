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
			TargetType = "Astroid.Providers.BinanceUsdFuturesProvider, Astroid.Providers"
		}, x => x.Name == "binance-usd-futures");

		await db.SaveChangesAsync();
	}
}