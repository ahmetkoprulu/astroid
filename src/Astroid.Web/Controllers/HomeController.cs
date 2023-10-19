using Microsoft.AspNetCore.Mvc;
using Astroid.Entity;
using Astroid.Web.Models;
using Astroid.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Astroid.Providers;
using Binance.Net.Objects;
using Binance.Net.Clients;
using Binance.Net;
using CryptoExchange.Net.Authentication;

namespace Astroid.Web;

[Route("api")]
public class HomeController : BaseController
{
	public ExchangeInfoStore ExchangeStore { get; set; }
	public HomeController(AstroidDb db, ExchangeInfoStore exchangeStore) : base(db) => ExchangeStore = exchangeStore;

	[HttpPost("sign-up")]
	public async Task<IActionResult> SignUp(AMSignUp model)
	{
		if (!ModelState.IsValid) return BadRequest("Invalid form");

		var isUserExist = await Db.Users.AnyAsync(x => x.Email == model.Email);
		if (isUserExist) return BadRequest("Email already in use");

		var user = new ADUser
		{
			Id = Guid.NewGuid(),
			Name = model.Name,
			Email = model.Email,
			CreatedDate = DateTime.UtcNow
		};
		user.PasswordHash = ACrypto.Hash(model.Password, user.Id.ToString());
		await Db.AddAsync(user);
		await Db.SaveChangesAsync();

		return Success(default, "User signed up successfully");
	}

	[HttpPost("sign-in")]
	public async Task<IActionResult> SignIn(AMSignIn model)
	{
		if (!ModelState.IsValid) return BadRequest("Invalid form");

		var user = await Db.Users.FirstOrDefaultAsync(x => x.Email == model.Email);
		if (user == null) return BadRequest("Invalid email or password");

		var hash = ACrypto.Hash(model.Password, user.Id.ToString());
		if (user.PasswordHash != hash) return BadRequest("Invalid email or password");

		await HttpContext.SignOutAsync(ACWeb.Authentication.DefaultSchema);
		await AuthenticateUser(user, Guid.NewGuid(), model.RememberMe);

		return Success(default, "User signed in successfully");
	}

	[HttpGet("sign-out")]
	public async Task<IActionResult> SignOut()
	{
		await HttpContext.SignOutAsync(ACWeb.Authentication.DefaultSchema);

		return Success(default, "User signed out successfully");
	}

	[HttpGet("exchange/{name}/symbols")]
	public async Task<IActionResult> GetSymbols(string name)
	{
		var exchange = await ExchangeStore.Get(name);
		var symbols = exchange?.Symbols.Select(x => x.BaseAsset ?? string.Empty) ?? new List<string>();

		return Success(symbols);
	}

	[HttpGet("status")]
	public async Task<IActionResult> Status()
	{
		var exchanges = await ExchangeStore.GetAll();

		return Ok(new
		{
			ServerTime = DateTime.UtcNow,
			Exchanges = exchanges
		});
	}

	[HttpGet("status/order-book/{exchange}/{ticker}")]
	public async Task<IActionResult> OrderBookStatus(string exchange, string ticker, [FromQuery(Name = "depth")] int depth = 1000)
	{
		var symbolInfo = ExchangeStore.GetSymbolInfo(exchange, ticker);
		if (symbolInfo == null) return BadRequest("Invalid ticker");

		var orderBook = await ExchangeStore.GetOrderBook(exchange, ticker);

		return Ok(new
		{
			Asks = (await orderBook.GetAsks(depth)).Select(x => new AMOrderBookEntry { Price = x.Key, Quantity = x.Value }).Take(depth),
			Bids = (await orderBook.GetBids(depth)).Select(x => new AMOrderBookEntry { Price = x.Key, Quantity = x.Value }).Take(depth)
		});
	}

	[HttpGet("status/symbol-info/{exchange}/{ticker}")]
	public async Task<IActionResult> GetSymbolInfoStatus(string exchange, string ticker)
	{
		var symbolInfo = await ExchangeStore.GetSymbolInfoLazy(exchange, ticker);
		if (symbolInfo == null) return BadRequest("Invalid ticker");

		return Ok(new
		{
			ServerTime = DateTime.UtcNow,
			SymbolInfo = symbolInfo
		});
	}

	[HttpGet("status/snapshot/{ticker}")]
	public async Task<IActionResult> Snapshot(string exchange, string ticker, [FromQuery(Name = "depth")] int depth = 100)
	{
		var key = Environment.GetEnvironmentVariable("ASTROID_BINANCE_KEY");
		var secret = Environment.GetEnvironmentVariable("ASTROID_BINANCE_SECRET");

		if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(secret))
			throw new Exception("Binance credentials not found.");

		var client = new BinanceRestClient(o =>
		{
			o.ApiCredentials = new ApiCredentials(key, secret);
			if (exchange == ACExchanges.BinanceUsdFuturesTest)
				o.Environment = BinanceEnvironment.Testnet;
		});

		var snapshot = await client.UsdFuturesApi.ExchangeData.GetOrderBookAsync(ticker, 200);

		return Ok(new
		{
			ServerTime = DateTime.UtcNow,
			Asks = snapshot.Data.Asks.Take(depth),
			Bids = snapshot.Data.Bids.Take(depth)
		});
	}
}
