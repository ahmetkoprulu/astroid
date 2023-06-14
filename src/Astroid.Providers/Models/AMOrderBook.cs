
using System.Collections.Concurrent;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Models;

namespace Astroid.Providers;

public class AMOrderBook
{
	public string Symbol { get; }
	public long LastUpdateTime { get; set; } = 0;
	private readonly IDictionary<decimal, decimal> _asks = new ConcurrentDictionary<decimal, decimal>();
	private readonly IDictionary<decimal, decimal> _bids = new ConcurrentDictionary<decimal, decimal>();
	private readonly List<IBinanceEventOrderBook> _buffer = new();
	private const decimal IgnoreVolumeValue = 0;

	public AMOrderBook(string symbol)
	{
		if (string.IsNullOrEmpty(symbol))
			throw new ArgumentException("Invalid symbol value", nameof(symbol));

		Symbol = symbol;
	}

	public IEnumerable<KeyValuePair<decimal, decimal>> GetAsks(int size, int skip = 0) => _asks.ToArray().OrderBy(x => x.Key).Skip(skip).Take(size);

	public IEnumerable<KeyValuePair<decimal, decimal>> GetBids(int size, int skip = 0) => _bids.ToArray().OrderByDescending(x => x.Key).Skip(skip).Take(size);

	public IEnumerable<decimal> GetAskPrices(int depth = 0) => _asks.Keys.OrderBy(x => x).Take(depth == 0 ? _asks.Count : depth);

	public IEnumerable<decimal> GetBidPrices(int depth = 0) => _bids.Keys.OrderByDescending(x => x).Take(depth == 0 ? _bids.Count : depth);

	// Since ToArray method is not thread safe, iterating over the keys array.
	// Even getting the keys array is thread safe, accessing to the dictionary is not. So, we need to use TryGetValue method.
	public (decimal, decimal) GetNthBestAsk(int n)
	{
		do
		{
			var minKey = _asks.Keys.OrderBy(x => x).Skip(n - 1).FirstOrDefault();
			if (minKey == 0) return (0, 0);

			var valueExists = _asks.TryGetValue(minKey, out var minValue);
			if (valueExists) return (minKey, minValue);
		} while (_asks.Count > 0);

		return (0, 0);
	}

	public (decimal, decimal) GetNthBestBid(int n)
	{
		do
		{
			var minKey = _bids.Keys.OrderByDescending(x => x).Skip(n - 1).FirstOrDefault();
			if (minKey == 0) return (0, 0);

			var valueExists = _bids.TryGetValue(minKey, out var minValue);
			if (valueExists) return (minKey, minValue);
		} while (_asks.Count > 0);

		return (0, 0);
	}

	public (decimal, decimal) GetBestAsk()
	{
		do
		{
			var minKey = _asks.Keys.Min();
			var valueExists = _asks.TryGetValue(minKey, out var minValue);

			if (valueExists) return (minKey, minValue);
		} while (_asks.Count > 0);

		return (0, 0);
	}

	public (decimal, decimal) GetBestBid()
	{
		do
		{
			var maxKey = _bids.Keys.Max();
			var valueExists = _bids.TryGetValue(maxKey, out var minValue);

			if (valueExists) return (maxKey, minValue);
		} while (_bids.Count > 0);

		return (0, 0);
	}

	public int GetGreatestAskPriceLessThan(decimal price)
	{
		var keys = _asks.Keys.OrderBy(x => x).ToArray();
		for (var i = 0; i < keys.Length; i++)
		{
			if (keys[i] > price) return i;
		}

		return -1;
	}

	public int GetLeastBidPriceGreaterThan(decimal price)
	{
		var keys = _bids.Keys.OrderByDescending(x => x).ToArray();
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
	public void ProcessUpdate(IBinanceEventOrderBook e)
	{
		// 2. Buffer the events you receive from the stream
		if (LastUpdateTime <= 0)
		{
			_buffer.Add(e);
			return;
		}

		// Consume buffered events after snapshot is loaded
		if (LastUpdateTime > 0 && _buffer.Count > 0)
		{
			_buffer.Add(e);
			_buffer.Sort((x, y) => x.LastUpdateId.CompareTo(y.LastUpdateId));
			_buffer.ForEach(x => UpdateDepth(x));
			_buffer.Clear();

			return;
		}

		UpdateDepth(e);
	}

	public void UpdateDepth(IBinanceEventOrderBook e)
	{
		// 4. Drop any event where u is less or equal lastUpdateId in the snapshot
		if (e.LastUpdateId <= LastUpdateTime)
			return;

		LastUpdateTime = e.LastUpdateId;
		UpdateOrderBook(e.Asks, _asks);
		UpdateOrderBook(e.Bids, _bids);
	}

	public void UpdateOrderBook(IEnumerable<BinanceOrderBookEntry> updates, IDictionary<decimal, decimal> orders)
	{
		foreach (var t in updates)
		{
			if (t.Quantity > IgnoreVolumeValue)
				orders[t.Price] = t.Quantity;
			// 8. If the quantity is 0, remove the price level
			// 9. Receiving an event that removes a price level that is not in your local order book can happen and is normal.
			else if (orders.ContainsKey(t.Price)) orders.Remove(t.Price);
		}
	}

	public void LoadSnapshot(IEnumerable<BinanceOrderBookEntry> asks, IEnumerable<BinanceOrderBookEntry> bids, long lastUpdateId)
	{
		_asks.Clear();
		_bids.Clear();

		LastUpdateTime = lastUpdateId;
		UpdateOrderBook(asks, _asks);
		UpdateOrderBook(bids, _bids);
	}
}
