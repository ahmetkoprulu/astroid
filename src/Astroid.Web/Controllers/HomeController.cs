using Microsoft.AspNetCore.Mvc;
using Astroid.Entity;
using Astroid.Web.Models;
using Astroid.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Astroid.Providers;

namespace Astroid.Web;

[Route("api")]
public class HomeController : BaseController
{
	public HomeController(AstroidDb db) : base(db) { }

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

	[HttpGet("status")]
	public IActionResult Status()
	{
		var exchanges = ExchangeInfoStore.GetAll();

		return Ok(new
		{
			ServerTime = DateTime.UtcNow,
			Exchanges = exchanges
		});
	}

	[HttpGet("status/order-book/{ticker}")]
	public async Task<IActionResult> OrderBookStatus(string ticker, [FromQuery(Name = "depth")] int depth = 1000)
	{
		var symbolInfo = ExchangeInfoStore.GetSymbolInfo(ACExchanges.BinanceUsdFutures, ticker);
		if (symbolInfo == null) return BadRequest("Invalid ticker");

		return Ok(new
		{
			Asks = symbolInfo.OrderBook?.GetAsks(depth).Select(x => new AMOrderBookEntry { Price = x.Key, Quantity = x.Value }).Take(depth),
			Bids = symbolInfo.OrderBook?.GetBids(depth).Select(x => new AMOrderBookEntry { Price = x.Key, Quantity = x.Value }).Take(depth)
		});
	}

	[HttpGet("status/snapshot/{ticker}")]
	public async Task<IActionResult> Snapshot(string ticker, [FromServices] BinanceCacheFeed feed, [FromQuery(Name = "depth")] int depth = 1000)
	{
		var snapshot = await feed.GetDepth(ticker);

		return Ok(new
		{
			ServerTime = DateTime.UtcNow,
			Asks = snapshot.Asks.Take(depth),
			Bids = snapshot.Bids.Take(depth)
		});
	}
}
