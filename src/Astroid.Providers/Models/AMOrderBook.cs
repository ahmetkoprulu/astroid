
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

	// ToArray is thread safe for ConcurrentDictionary so the collection can be ordered safely
	public IEnumerable<KeyValuePair<decimal, decimal>> GetAsks(int size) => _asks.ToArray().OrderBy(x => x.Key).Take(size);

	public IEnumerable<KeyValuePair<decimal, decimal>> GetBids(int size) => _bids.ToArray().OrderByDescending(x => x.Key).Take(size);

	public KeyValuePair<decimal, decimal> GetNthAsk(int n) => _asks.ToArray().OrderBy(x => x.Key).Skip(n - 1).FirstOrDefault();

	public KeyValuePair<decimal, decimal> GetNthBid(int n) => _bids.ToArray().OrderByDescending(x => x.Key).Skip(n - 1).FirstOrDefault();

	public KeyValuePair<decimal, decimal> GetFirstAsk() => _asks.ToArray().OrderBy(x => x.Key).FirstOrDefault();

	public KeyValuePair<decimal, decimal> GetFirstBid() => _bids.ToArray().OrderByDescending(x => x.Key).FirstOrDefault();

	// How to manage a local order book correctly [1]:
	//   1. Open a stream to wss://stream.binance.com:9443/ws/bnbbtc@depth
	// + 2. Buffer the events you receive from the stream
	// + 3. Get a depth snapshot from https://www.binance.com/api/v1/depth?symbol=BNBBTC&amp;limit=1000
	// + 4. Drop any event where u is less or equal lastUpdateId in the snapshot
	//   5. The first processed should have U less or equal lastUpdateId+1 AND u equal or greater lastUpdateId+1
	// + 6. While listening to the stream, each new event's U should be equal to the previous event's u+1
	// + 7. The data in each event is the absolute quantity for a price level
	// + 8. If the quantity is 0, remove the price level
	// + 9. Receiving an event that removes a price level that is not in your local order book can happen and is normal.
	// Reference:
	//     1. https://github.com/binance/binance-spot-api-docs/blob/master/web-socket-streams.md#how-to-manage-a-local-order-book-correctly
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
