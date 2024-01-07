using Astroid.Core;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Astroid.Providers.Extentions;
using CryptoExchange.Net.Authentication;

namespace Astroid.Providers;

public class BinanceUsdFuturesProvider : ExchangeFuturesProviderBase
{
	[PropertyMetadata("API Key", Type = PropertyTypes.Text, Required = true, Encrypted = true, Group = "General")]
	public string Key { get; set; } = string.Empty;

	[PropertyMetadata("API Secret", Type = PropertyTypes.Text, Required = true, Encrypted = true, Group = "General")]
	public string Secret { get; set; } = string.Empty;

	[PropertyMetadata("Test Net", Type = PropertyTypes.Boolean, Group = "General")]
	public bool IsTestNet { get; set; } = false;

	private BinanceRestClient Client { get; set; }

	public BinanceUsdFuturesProvider(ExecutionRepository repo, ExchangeInfoStore infoStore, ExchangeCalculator calculator, MetadataMapper metadataMapper, BinanceRestClient client) : base(repo, infoStore, calculator, metadataMapper) => Client = client;

	public override void Context(string properties)
	{
		base.Context(properties);
		Client.UsdFuturesApi.SetApiCredentials(new ApiCredentials(Key, Secret));
	}

	protected override async Task<AMOrderResult> PlaceMarketOrder(string ticker, decimal quantity, OrderType oType, PositionType pType, bool reduceOnly = false)
	{
		var orderResponse = await Client.UsdFuturesApi.Trading
			.PlaceOrderAsync(
				ticker,
				oType.ToBinance(pType),
				FuturesOrderType.Market,
				quantity,
				positionSide: pType.ToBinance(),
				workingType: WorkingType.Contract,
				orderResponseType: OrderResponseType.Result
			);

		if (!orderResponse.Success)
		{
			// result.WithMessage($"Failed Market Order Placing Order: {orderResponse?.Error?.Message}").AddAudit(AuditType.OpenOrderPlaced, $"Failed placing market order: {orderResponse?.Error?.Message}", data: JsonConvert.SerializeObject(new { Ticker = ticker, Quantity = quantity, OrderType = oSide.ToString(), PositionType = pSide.ToString() }));
			return AMOrderResult.WithFailure(orderResponse?.Error?.Message);
		}
		// result.WithSuccess().AddAudit(AuditType.OpenOrderPlaced, $"Placed market order successfully.", JsonConvert.SerializeObject(new { Ticker = ticker, Quantity = quantity, OrderType = oSide.ToString(), PositionType = pSide.ToString() }));
		return AMOrderResult.WithSuccess(orderResponse.Data.AveragePrice, orderResponse.Data.QuantityFilled, orderResponse.Data.Id.ToString());
	}

	protected override async Task<AMOrderResult> PlaceOrderTillPositionFilled(AMOrderBook orderBook, string ticker, decimal quantity, OrderType oType, PositionType pType, LimitSettings settings)
	{
		var remainingQuantity = quantity; var lastEntryPrice = 0m; var totalQuantity = 0m; var i = 0; var cts = new CancellationTokenSource(settings.ForceTimeout * 1000);
		while (remainingQuantity > 0)
		{
			if (cts.IsCancellationRequested) return AMOrderResult.WithFailure($"Force order book limit order timed out.");

			var (p, q) = pType == PositionType.Long ? await orderBook.GetBestAsk() : await orderBook.GetBestBid();
			if (p <= 0 || q <= 0) continue;

			var orderQuantity = Math.Min(remainingQuantity, q);
			var orderResponse = await Client.UsdFuturesApi.Trading
				.PlaceOrderAsync(
					ticker,
					oType.ToBinance(pType),
					FuturesOrderType.Limit,
					orderQuantity,
					p,
					positionSide: pType.ToBinance(),
					timeInForce: TimeInForce.FillOrKill,
					workingType: WorkingType.Contract,
					orderResponseType: OrderResponseType.Result
				);

			totalQuantity += orderResponse.Data.QuantityFilled;
			i++;

			if (!orderResponse.Success || orderResponse.Data.AveragePrice <= 0)
			{
				await Task.Delay(300);
				continue;
			}

			lastEntryPrice = orderResponse.Data.AveragePrice;
			remainingQuantity -= orderResponse.Data.QuantityFilled;
		}

		return AMOrderResult.WithSuccess(lastEntryPrice, totalQuantity);
	}

	protected override async Task<AMOrderResult> PlaceDeviatedOrder(string ticker, decimal quantity, decimal price, OrderType oType, PositionType pType)
	{
		var orderResponse = await Client.UsdFuturesApi.Trading
			.PlaceOrderAsync(
				ticker,
				oType.ToBinance(pType),
				FuturesOrderType.Limit,
				quantity,
				price,
				positionSide: pType.ToBinance(),
				timeInForce: TimeInForce.FillOrKill,
				workingType: WorkingType.Contract,
				orderResponseType: OrderResponseType.Result
			);

		if (!orderResponse.Success) return AMOrderResult.WithFailure(orderResponse?.Error?.Message);
		// result.WithSuccess().AddAudit(AuditType.OpenOrderPlaced, $"Placed limit order successfully.", JsonConvert.SerializeObject(new { Ticker = ticker, EntryPrice = price, Quantity = quantity, OrderType = oSide.ToString(), PositionType = pSide.ToString() }));
		return AMOrderResult.WithSuccess(orderResponse.Data.AveragePrice, orderResponse.Data.QuantityFilled, orderResponse.Data.Id.ToString());
	}

	protected override async Task<AMOrderResult> PlaceOboOrder(AMOrderBook orderBook, string ticker, decimal quantity, OrderType oType, PositionType pType, LimitSettings settings)
	{
		var entryIndex = 1;
		if (settings.ComputeEntryPoint) entryIndex = await Calculator.CalculateEntryPointIndexAsync(orderBook, pType, settings);

		var endIndex = settings.OrderBookOffset + entryIndex;
		for (var i = entryIndex; i <= endIndex; i++)
		{
			var (p, _) = pType == PositionType.Long ? await orderBook.GetNthBestAsk(settings.OrderBookSkip + i) : await orderBook.GetNthBestBid(settings.OrderBookSkip + i);
			var orderResponse = await Client.UsdFuturesApi.Trading
				.PlaceOrderAsync(
					ticker,
					oType.ToBinance(pType),
					FuturesOrderType.Limit,
					quantity,
					p,
					positionSide: pType.ToBinance(),
					timeInForce: TimeInForce.FillOrKill,
					workingType: WorkingType.Contract,
					orderResponseType: OrderResponseType.Result
				);

			if (orderResponse.Success) return AMOrderResult.WithSuccess(orderResponse.Data.AveragePrice, orderResponse.Data.QuantityFilled, orderResponse.Data.Id.ToString());
		}

		return AMOrderResult.WithFailure($"Failed placing OBO limit order");
	}

	protected override async Task<AMOrderBook> GetOrderBook(AMOrderBook ob, string ticker)
	{
		var orderBookResponse = await Client.UsdFuturesApi.ExchangeData.GetOrderBookAsync(ticker);
		if (!orderBookResponse.Success) throw new Exception($"Failed getting order book: {orderBookResponse?.Error?.Message}");

		return ob.LoadSnapshotLocaly(
				orderBookResponse.Data.Asks.Select(x => new AMOrderBookEntry { Price = x.Price, Quantity = x.Quantity }),
				orderBookResponse.Data.Bids.Select(x => new AMOrderBookEntry { Price = x.Price, Quantity = x.Quantity }),
				1);
	}

	protected override async Task<IEnumerable<AMExchangePosition>> GetPositions()
	{
		var response = await Client.UsdFuturesApi.Account.GetPositionInformationAsync();
		if (!response.Success)
			throw new Exception($"Failed getting position information: {response?.Error?.Message}");

		return response.Data.Select(x => new AMExchangePosition
		{
			EntryPrice = x.EntryPrice,
			Quantity = x.Quantity,
			Type = x.PositionSide.ToAstroid(),
			Symbol = x.Symbol
		});
	}

	public override async Task<AMExchangeWallet> GetAccountInfo(Guid id, string name)
	{
		var wallet = new AMExchangeWallet { Id = id, Name = name };
		var res = await Client.UsdFuturesApi.Account.GetAccountInfoAsync();
		if (!res.Success)
		{
			wallet.ErrorMessage = res?.Error?.Message;
			return wallet;
		}

		foreach (var asset in res.Data.Assets)
		{
			if (asset.WalletBalance <= 0) continue;

			wallet.Assets.Add(new AMExchangeWalletAsset { Name = asset.Asset, Balance = asset.AvailableBalance });
		}

		wallet.IsHealthy = true;
		wallet.TotalBalance = res.Data.TotalWalletBalance;
		wallet.UnrealizedPnl = res.Data.TotalUnrealizedProfit;

		//IDEA: Add unrealized and realized pnl
		//IDEA: Add positions

		return wallet;
	}

	protected override async Task<decimal> GetBalance(string asset)
	{
		var balancesResponse = await Client.UsdFuturesApi.Account.GetBalancesAsync();
		if (!balancesResponse.Success) throw new Exception($"Could not get account balances: {balancesResponse?.Error?.Message}");

		var balanceInfo = balancesResponse.Data.FirstOrDefault(x => x.Asset == asset) ?? throw new Exception($"Could not find {asset} balance");

		return balanceInfo.AvailableBalance;
	}

	protected override async Task ChangeLeverage(string ticker, int leverage)
	{
		var response = await Client.UsdFuturesApi.Account.ChangeInitialLeverageAsync(ticker, leverage);
		if (!response.Success) throw new Exception($"Failed changing leverage: {response.Error?.Message}");
	}

	protected override async Task ChangeMarginType(string ticker, MarginType marginType, AMProviderResult result)
	{
		var response = await Client.UsdFuturesApi.Account.ChangeMarginTypeAsync(ticker, (FuturesMarginType)marginType);

		if (!response.Success) result.WithMessage($"Could not change margin type for {ticker} to {marginType}: {response?.Error?.Message}");
		else result.WithMessage($"Changed margin type for {ticker} to {marginType} successfully");
	}

	public override void Dispose()
	{
		base.Dispose();
		Client.Dispose();
		GC.SuppressFinalize(this);
	}
}
