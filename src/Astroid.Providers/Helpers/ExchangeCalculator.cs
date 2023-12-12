using Astroid.Core;

public class ExchangeCalculator
{
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
}
