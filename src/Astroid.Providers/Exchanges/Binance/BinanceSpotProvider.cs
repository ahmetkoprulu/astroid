using Astroid.Core;
using Astroid.Providers.Extentions;
using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Enums;
using CryptoExchange.Net.Authentication;

namespace Astroid.Providers;

public class BinanceSpotProvider : ExchangeSpotProviderBase
{
	[PropertyMetadata("API Key", Type = PropertyTypes.Text, Required = true, Encrypted = true, Group = "General")]
	public string Key { get; set; } = string.Empty;

	[PropertyMetadata("API Secret", Type = PropertyTypes.Text, Required = true, Encrypted = true, Group = "General")]
	public string Secret { get; set; } = string.Empty;

	private BinanceRestClient Client { get; set; }

	public BinanceSpotProvider(ExecutionRepository repo, ExchangeInfoStore infoStore, ExchangeCalculator calculator, MetadataMapper metadataMapper, BinanceRestClient client) : base(repo, infoStore, calculator, metadataMapper) => Client = client;

	public override void Context(string properties)
	{
		base.Context(properties);
		Client.SpotApi.SetApiCredentials(new ApiCredentials(Key, Secret));
	}

	public override async Task<AMExchangeWallet> GetAccountInfo(Guid id, string name)
	{
		var wallet = new AMExchangeWallet { Id = id, Name = name };
		var res = await Client.SpotApi.Account.GetAccountInfoAsync();
		if (!res.Success)
		{
			wallet.ErrorMessage = res?.Error?.Message;
			return wallet;
		}

		foreach (var asset in res.Data.Balances)
		{
			if (asset.Available <= 0) continue;

			wallet.Assets.Add(new AMExchangeWalletAsset { Name = asset.Asset, Balance = asset.Available });
		}

		wallet.IsHealthy = true;
		wallet.TotalBalance = 0;
		wallet.UnrealizedPnl = 0;

		//IDEA: Add unrealized and realized pnl
		//IDEA: Add positions

		return wallet;
	}

	protected override async Task<decimal> GetBalance(string asset)
	{
		var balancesResponse = await Client.SpotApi.Account.GetBalancesAsync();
		if (!balancesResponse.Success) throw new Exception($"Could not get account balances: {balancesResponse?.Error?.Message}");

		var balanceInfo = balancesResponse.Data.FirstOrDefault(x => x.Asset == asset) ?? throw new Exception($"Could not find {asset} balance");

		return balanceInfo.Available;
	}

	protected override async Task<AMOrderBook> GetOrderBook(AMOrderBook orderBook, string ticker)
	{
		var orderBookResponse = await Client.SpotApi.ExchangeData.GetOrderBookAsync(ticker);
		if (!orderBookResponse.Success) throw new Exception($"Failed getting order book: {orderBookResponse?.Error?.Message}");

		return orderBook.LoadSnapshotLocaly(
				orderBookResponse.Data.Asks.Select(x => new AMOrderBookEntry { Price = x.Price, Quantity = x.Quantity }),
				orderBookResponse.Data.Bids.Select(x => new AMOrderBookEntry { Price = x.Price, Quantity = x.Quantity }),
				1);
	}

	protected override async Task<AMOrderResult> PlaceDeviatedOrder(string ticker, decimal quantity, decimal price, OrderType oType, PositionType pType)
	{
		var orderResponse = await Client.SpotApi.Trading
			.PlaceOrderAsync(
				ticker,
				oType.ToBinance(pType),
				SpotOrderType.Limit,
				quantity,
				price,
				timeInForce: TimeInForce.FillOrKill,
				orderResponseType: OrderResponseType.Result
			);

		if (!orderResponse.Success) return AMOrderResult.WithFailure(orderResponse?.Error?.Message);
		// result.WithSuccess().AddAudit(AuditType.OpenOrderPlaced, $"Placed limit order successfully.", JsonConvert.SerializeObject(new { Ticker = ticker, EntryPrice = price, Quantity = quantity, OrderType = oSide.ToString(), PositionType = pSide.ToString() }));
		return AMOrderResult.WithSuccess(orderResponse.Data.AverageFillPrice!.Value, orderResponse.Data.QuantityFilled, orderResponse.Data.Id.ToString());
	}

	protected override async Task<AMOrderResult> PlaceMarketOrder(string ticker, decimal quantity, OrderType oType, PositionType pType, bool reduceOnly = false)
	{
		var orderResponse = await Client.SpotApi.Trading
			.PlaceOrderAsync(
				ticker,
				oType.ToBinance(pType),
				SpotOrderType.Market,
				quantity,
				orderResponseType: OrderResponseType.Result
			);

		if (!orderResponse.Success)
		{
			// result.WithMessage($"Failed Market Order Placing Order: {orderResponse?.Error?.Message}").AddAudit(AuditType.OpenOrderPlaced, $"Failed placing market order: {orderResponse?.Error?.Message}", data: JsonConvert.SerializeObject(new { Ticker = ticker, Quantity = quantity, OrderType = oSide.ToString(), PositionType = pSide.ToString() }));
			return AMOrderResult.WithFailure(orderResponse?.Error?.Message, orderResponse?.Error?.Code.ToErrorCode() ?? default);
		}
		// result.WithSuccess().AddAudit(AuditType.OpenOrderPlaced, $"Placed market order successfully.", JsonConvert.SerializeObject(new { Ticker = ticker, Quantity = quantity, OrderType = oSide.ToString(), PositionType = pSide.ToString() }));
		return AMOrderResult.WithSuccess(orderResponse.Data.AverageFillPrice!.Value, orderResponse.Data.QuantityFilled, orderResponse.Data.Id.ToString());
	}

	protected override async Task<AMOrderResult> PlaceOboOrder(AMOrderBook orderBook, string ticker, decimal quantity, OrderType oType, PositionType pType, LimitSettings settings)
	{
		var entryIndex = 1;
		if (settings.ComputeEntryPoint) entryIndex = await Calculator.CalculateEntryPointIndexAsync(orderBook, pType, settings);

		var endIndex = settings.OrderBookOffset + entryIndex;
		for (var i = entryIndex; i <= endIndex; i++)
		{
			var (p, _) = pType == PositionType.Long ? await orderBook.GetNthBestAsk(settings.OrderBookSkip + i) : await orderBook.GetNthBestBid(settings.OrderBookSkip + i);
			var orderResponse = await Client.SpotApi.Trading
				.PlaceOrderAsync(
					ticker,
					oType.ToBinance(pType),
					SpotOrderType.Limit,
					quantity,
					p,
					timeInForce: TimeInForce.FillOrKill,
					orderResponseType: OrderResponseType.Result
				);

			if (orderResponse.Success) return AMOrderResult.WithSuccess(orderResponse.Data.AverageFillPrice!.Value, orderResponse.Data.QuantityFilled, orderResponse.Data.Id.ToString());
		}

		return AMOrderResult.WithFailure($"Failed placing OBO limit order");
	}

	protected override async Task<AMOrderResult> PlaceOrderTillPositionFilled(AMOrderBook orderBook, string ticker, decimal quantity, OrderType oType, PositionType pType, LimitSettings settings)
	{
		var remainingQuantity = quantity; var lastEntryPrice = 0m; var totalQuantity = 0m; var i = 0; var cts = new CancellationTokenSource(settings.ForceTimeout * 1000);
		while (remainingQuantity > 0)
		{
			if (cts.IsCancellationRequested) return AMOrderResult.WithFailure($"Force order book limit order timed out.");

			var (p, q) = await orderBook.GetBestAsk();
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
}
