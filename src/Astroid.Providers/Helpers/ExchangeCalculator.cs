using Astroid.Core;
using Astroid.Providers;

public class ExchangeCalculator
{
	public CodeExecutor CodeExecutor { get; set; }

	public ExchangeCalculator(CodeExecutor cExecutor) => CodeExecutor = cExecutor;

	public decimal CalculatePnl(decimal entryPrice, decimal exitPrice, decimal quantity, int leverage, PositionType type)
	{
		var distance = CalculateDistanceFromEntry(entryPrice, exitPrice, type);

		return distance * quantity * leverage;
	}

	public decimal CalculateStopLoss(decimal activation, decimal entryPrice, int precision, PositionType type)
	{
		if (type == PositionType.Long) return Math.Round(entryPrice - (entryPrice * activation / 100), precision);

		return Math.Round(entryPrice + (entryPrice * activation / 100), precision);
	}

	public decimal CalculatePyramid(decimal activation, decimal entryPrice, int precision, PositionType type)
	{
		var isDecreasing = activation < 0;
		if (type == PositionType.Long)
		{
			if (isDecreasing) return Math.Round(entryPrice - (entryPrice * Math.Abs(activation) / 100), precision);
			return Math.Round(entryPrice + (entryPrice * Math.Abs(activation) / 100), precision);
		}

		if (isDecreasing) return Math.Round(entryPrice + (entryPrice * Math.Abs(activation) / 100), precision);
		return Math.Round(entryPrice - (entryPrice * Math.Abs(activation) / 100), precision);
	}

	public decimal CalculateTakeProfit(decimal activation, decimal entryPrice, int precision, PositionType type)
	{
		if (type == PositionType.Long) return Math.Round(entryPrice + (entryPrice * activation / 100), precision);

		return Math.Round(entryPrice - (entryPrice * activation / 100), precision);
	}

	public decimal CalculateStandardDeviation(IEnumerable<decimal> prices)
	{
		var mean = prices.Average();
		var squaredDifferences = prices.Select(p => Math.Pow((double)p - (double)mean, 2));
		var variance = squaredDifferences.Sum() / squaredDifferences.Count();
		var standardDeviation = Math.Sqrt(variance);

		return (decimal)standardDeviation;
	}

	public decimal CalculateAssetQuantity(decimal? size, PositionSizeType type, decimal lastPrice, int precision, decimal amount)
	{
		if (!size.HasValue || size <= 0) return amount;
		if (type == PositionSizeType.FixedInAsset) return size.Value;
		if (type == PositionSizeType.FixedInUsd) return Math.Round(size.Value / lastPrice, precision);

		return amount * size.Value / 100;
	}

	public decimal CalculateDeviatedPrice(decimal price, int precision, decimal deviation, PositionType pType)
	{
		var deviatedDifference = price * deviation / 100;
		return pType == PositionType.Long ? Math.Round(price + deviatedDifference, precision - 1) : Math.Round(price - deviatedDifference, precision - 1);
	}

	public async Task<decimal> CalculateEntryPointAsync(AMOrderBook orderBook, PositionType pType, LimitSettings settings)
	{
		if (settings.ComputationMethod == OrderBookComputationMethod.Code)
		{
			var entries = await orderBook.GetEntries(pType, settings.OrderBookDepth);
			var result = CodeExecutor.ExecuteComputationMethod(settings.Code, entries);
			if (!result.IsSuccess) throw new Exception(result.Message);

			return result.Data;
		}

		var prices = await orderBook.GetPrices(pType, settings.OrderBookDepth);
		var sDeviation = CalculateStandardDeviation(prices);
		var mean = prices.Average();

		return pType == PositionType.Long ? mean + (2 * sDeviation) : mean - (2 * sDeviation);
	}

	public async Task<int> CalculateEntryPointIndexAsync(AMOrderBook orderBook, PositionType pSide, LimitSettings settings)
	{
		var entryPoint = await CalculateEntryPointAsync(orderBook, pSide, settings);
		var i = await orderBook.GetClosestPriceIndex(entryPoint, pSide);

		if (i <= 0) throw new Exception("Could not find entry point out of order book.");

		return i;
	}

	private decimal CalculateDistanceFromEntry(decimal entryPrice, decimal exitPrice, PositionType type)
	{
		if (type == PositionType.Long) return exitPrice - entryPrice;

		return entryPrice - exitPrice;
	}

	public decimal CalculateWeightedAveragePrice(decimal weightedPrice, decimal quantity) => weightedPrice / quantity;
}
