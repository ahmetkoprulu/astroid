
using System.Collections.Concurrent;
using Astroid.Core.Cache;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Models;

namespace Astroid.Providers;

public class AMOrderBook
{
	public ICacheService Cache { get; set; }
	public string Symbol { get; }
	public string Exchange { get; set; } = string.Empty;
	public DateTime LastUpdateDate { get; private set; } = DateTime.MinValue;
	private readonly List<IBinanceEventOrderBook> _buffer = new();
	private const decimal IgnoreVolumeValue = 0;

	public AMOrderBook(string exchange, string symbol, ICacheService cache)
	{
		Cache = cache;
		if (string.IsNullOrEmpty(symbol))
			throw new ArgumentException("Invalid symbol value", nameof(symbol));

		Symbol = symbol;
		Exchange = exchange;
	}

	public async Task SetLastUpdateTime(long timestamp)
	{
		await WriteLastUpdateTime(timestamp);
		LastUpdateDate = DateTime.UtcNow;
	}

	public async Task<IEnumerable<KeyValuePair<decimal, decimal>>> GetAsks(int size, int skip = 0)
	{
		var asks = await ReadAsks();
		return asks.ToArray().OrderBy(x => x.Key).Skip(skip).Take(size);
	}

	public async Task<IEnumerable<KeyValuePair<decimal, decimal>>> GetBids(int size, int skip = 0)
	{
		var bids = await ReadBids();
		return bids.ToArray().OrderByDescending(x => x.Key).Skip(skip).Take(size);
	}

	public async Task<IEnumerable<decimal>> GetAskPrices(int depth = 0)
	{
		var asks = await ReadAsks();
		return asks.Keys.OrderBy(x => x).Take(depth == 0 ? asks.Count : depth);
	}

	public async Task<IEnumerable<decimal>> GetBidPrices(int depth = 0)
	{
		var bids = await ReadBids();
		return bids.Keys.OrderByDescending(x => x).Take(depth == 0 ? bids.Count : depth);
	}

	// Since ToArray method is not thread safe, iterating over the keys array.
	// Even getting the keys array is thread safe, accessing to the dictionary is not. So, we need to use TryGetValue method.
	public async Task<(decimal, decimal)> GetNthBestAsk(int n)
	{
		var asks = await ReadAsks();

		do
		{
			var minKey = asks.Keys.OrderBy(x => x).Skip(n - 1).FirstOrDefault();
			if (minKey == 0) return (0, 0);

			var valueExists = asks.TryGetValue(minKey, out var minValue);
			if (valueExists) return (minKey, minValue);
		} while (asks.Count > 0);

		return (0, 0);
	}

	public async Task<(decimal, decimal)> GetNthBestBid(int n)
	{
		var bids = await ReadBids();

		do
		{
			var minKey = bids.Keys.OrderByDescending(x => x).Skip(n - 1).FirstOrDefault();
			if (minKey == 0) return (0, 0);

			var valueExists = bids.TryGetValue(minKey, out var minValue);
			if (valueExists) return (minKey, minValue);
		} while (bids.Count > 0);

		return (0, 0);
	}

	public async Task<(decimal, decimal)> GetBestAsk()
	{
		var asks = await ReadAsks();

		do
		{
			var minKey = asks.Keys.Min();
			var valueExists = asks.TryGetValue(minKey, out var minValue);

			if (valueExists) return (minKey, minValue);
		} while (asks.Count > 0);

		return (0, 0);
	}

	public async Task<(decimal, decimal)> GetBestBid()
	{
		var bids = await ReadBids();

		do
		{
			var maxKey = bids.Keys.Max();
			var valueExists = bids.TryGetValue(maxKey, out var minValue);

			if (valueExists) return (maxKey, minValue);
		} while (bids.Count > 0);

		return (0, 0);
	}

	public async Task<int> GetGreatestAskPriceLessThan(decimal price)
	{
		var asks = await ReadAsks();
		var keys = asks.Keys.OrderBy(x => x).ToArray();
		for (var i = 0; i < keys.Length; i++)
		{
			if (keys[i] > price) return i;
		}

		return -1;
	}

	public async Task<int> GetLeastBidPriceGreaterThan(decimal price)
	{
		var bids = await ReadBids();
		var keys = bids.Keys.OrderByDescending(x => x).ToArray();
		for (var i = 0; i < keys.Length; i++)
		{
			if (keys[i] < price) return i;
		}

		return -1;
	}

	// How to manage a local order book correctly:
	//   1. Open a stream to wss://stream.binance.com:9443/ws/bnbbtc@depth
	// + 2. Buffer the events you receive from the stream
	// + 3. Get a depth snapshot from https://www.binance.com/api/v1/depth?symbol=BNBBTC&amp;limit=1000
	// + 4. Drop any event where u is less or equal lastUpdateId in the snapshot
	//   5. The first processed should have U less or equal lastUpdateId+1 AND u equal or greater lastUpdateId+1
	// + 6. While listening to the stream, each new event's U should be equal to the previous event's u+1
	// + 7. The data in each event is the absolute quantity for a price level
	// + 8. If the quantity is 0, remove the price level
	// + 9. Receiving an event that removes a price level that is not in your local order book can happen and is normal.
	// Reference: https://github.com/binance/binance-spot-api-docs/blob/master/web-socket-streams.md#how-to-manage-a-local-order-book-correctly
	public async Task ProcessUpdate(IBinanceEventOrderBook e)
	{
		// 2. Buffer the events you receive from the stream
		var lastUpdateTime = await ReadLastUpdateTime();
		if (lastUpdateTime <= 0)
		{
			_buffer.Add(e);
			return;
		}

		// Consume buffered events after snapshot is loaded
		if (lastUpdateTime > 0 && _buffer.Count > 0)
		{
			_buffer.Add(e);
			_buffer.Sort((x, y) => x.LastUpdateId.CompareTo(y.LastUpdateId));

			foreach (var obe in _buffer)
				await UpdateDepth(obe);

			_buffer.Clear();
			return;
		}

		await UpdateDepth(e);
	}

	public async Task UpdateDepth(IBinanceEventOrderBook e)
	{
		// 4. Drop any event where u is less or equal lastUpdateId in the snapshot
		var lastUpdateTime = await ReadLastUpdateTime();
		if (e.LastUpdateId <= lastUpdateTime)
			return;

		await SetLastUpdateTime(e.LastUpdateId);
		var asks = await ReadAsks();
		var updatedAsks = UpdateOrderBook(e.Asks, asks);
		await WriteAsks(updatedAsks);

		var bids = await ReadBids();
		var updatedBids = UpdateOrderBook(e.Bids, bids);
		await WriteBids(updatedBids);
	}

	public IDictionary<decimal, decimal> UpdateOrderBook(IEnumerable<BinanceOrderBookEntry> updates, IDictionary<decimal, decimal> orders)
	{
		foreach (var t in updates)
		{
			if (t.Quantity > IgnoreVolumeValue)
				orders[t.Price] = t.Quantity;
			// 8. If the quantity is 0, remove the price level
			// 9. Receiving an event that removes a price level that is not in your local order book can happen and is normal.
			else if (orders.ContainsKey(t.Price)) orders.Remove(t.Price);
		}

		return orders;
	}

	public async Task LoadSnapshot(IEnumerable<BinanceOrderBookEntry> asks, IEnumerable<BinanceOrderBookEntry> bids, long lastUpdateId)
	{
		await SetLastUpdateTime(lastUpdateId);
		await WriteAsks(asks.ToDictionary(x => x.Price, x => x.Quantity));
		await WriteBids(bids.ToDictionary(x => x.Price, x => x.Quantity));
	}

	public async Task<IDictionary<decimal, decimal>> ReadAsks()
	{
		var asks = await Cache.Get<IDictionary<decimal, decimal>>($"OrderBook:{Exchange}:{Symbol}:Asks");
		if (asks == null) return new ConcurrentDictionary<decimal, decimal>();

		return asks;
	}

	public async Task<IDictionary<decimal, decimal>> ReadBids()
	{
		var bids = await Cache.Get<IDictionary<decimal, decimal>>($"OrderBook:{Exchange}:{Symbol}:Bids");
		if (bids == null) return new ConcurrentDictionary<decimal, decimal>();

		return bids;
	}

	public async Task<long> ReadLastUpdateTime() => await Cache.Get<long>($"OrderBook:{Exchange}:{Symbol}:LastUpdateTime", 0);

	public async Task WriteLastUpdateTime(long lastUpdateTime) => await Cache.Set($"OrderBook:{Exchange}:{Symbol}:LastUpdateTime", lastUpdateTime, TimeSpan.FromSeconds(30));

	public async Task WriteAsks(IDictionary<decimal, decimal> asks) => await Cache.Set($"OrderBook:{Exchange}:{Symbol}:Asks", asks, TimeSpan.FromSeconds(30));

	public async Task WriteBids(IDictionary<decimal, decimal> bids) => await Cache.Set($"OrderBook:{Exchange}:{Symbol}:Bids", bids, TimeSpan.FromSeconds(30));
}
