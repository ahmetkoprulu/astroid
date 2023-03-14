using Microsoft.AspNetCore.Mvc;
using Astroid.Entity;
using Astroid.Web.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Astroid.Web.Helpers;
using Astroid.Providers.Extentions;
using Astroid.Core.Cache;
using Astroid.Core;
using Astroid.Providers;

namespace Astroid.Web;

public class ExchangesController : SecureController
{
	public ExchangesController(AstroidDb db, ICacheService cache) : base(db, cache) { }

	[HttpPost("list")]
	public async Task<IActionResult> List([FromBody] MPViewDataList<ADExchange> model)
	{
		model ??= new MPViewDataList<ADExchange>();

		model = await Db.Exchanges
			.Where(x => x.UserId == CurrentUser.Id)
			.AsNoTracking()
			.Include(x => x.Provider)
			.OrderByDescending(x => x.CreatedDate)
			.ViewDataListAsync<ADExchange>(model);

		return Success(model.ForJson(x => new AMExchange
		{
			Id = x.Id,
			Name = x.Label,
			Description = x.Description,
			ProviderId = x.Provider.Id,
			ProviderName = x.Provider.Title
		}));
	}

	[HttpGet]
	public async Task<IActionResult> Get()
	{
		var exchanges = await Db.Exchanges
			.Where(x => x.UserId == CurrentUser.Id)
			.AsNoTracking()
			.Include(x => x.Provider)
			.Select(x => new AMExchange
			{
				Id = x.Id,
				Name = x.Label,
				ProviderId = x.Provider.Id,
				ProviderName = x.Provider.Title
			})
			.ToListAsync();

		return Success(exchanges);
	}

	[HttpGet("{id}")]
	public async Task<IActionResult> Get(Guid id)
	{
		if (id == Guid.Empty)
			return BadRequest("Invalid exchange id");

		var exchange = await Db.Exchanges
			.Where(x => x.UserId == CurrentUser.Id)
			.AsNoTracking()
			.Include(x => x.Provider)
			.FirstOrDefaultAsync(x => x.Id == id);
		if (exchange == null)
			return NotFound("Exchange not found");

		return Success(new AMExchange
		{
			Id = exchange.Id,
			Name = exchange.Label,
			Description = exchange.Description,
			ProviderId = exchange.Provider.Id,
			ProviderName = exchange.Provider.Title,
			Properties = exchange.Properties
		});
	}

	[HttpPost("save")]
	public async Task<IActionResult> Save([FromBody] AMExchange model)
	{
		var isNameExists = await Db.Exchanges.AnyAsync(x => x.Label == model.Name && x.Id != model.Id);
		if (isNameExists) BadRequest("Exchange name already exists");

		if (model.Id == Guid.Empty)
		{
			var exchange = new ADExchange
			{
				Id = Guid.NewGuid(),
				Label = model.Name,
				Description = model.Description,
				Properties = model.Properties,
				ProviderId = model.ProviderId,
				CreatedDate = DateTime.Now,
				UserId = CurrentUser.Id
			};

			await Db.Exchanges.AddAsync(exchange);
		}
		else
		{
			var exchange = await Db.Exchanges.FirstOrDefaultAsync(x => x.Id == model.Id);
			if (exchange == null)
				return NotFound("Exchange not found");

			exchange.Label = model.Name;
			exchange.Description = model.Description;
			exchange.Properties = model.Properties;
			exchange.ProviderId = model.ProviderId;
			exchange.ModifiedDate = DateTime.Now;
		}

		await Db.SaveChangesAsync();

		return Success("Exchange saved successfully");
	}

	[HttpDelete("{id}")]
	public async Task<IActionResult> Delete(Guid id)
	{
		if (id == Guid.Empty)
			return BadRequest("Invalid exchange id");

		var exchange = await Db.Exchanges.FirstOrDefaultAsync(x => x.Id == id && x.UserId == CurrentUser.Id);
		if (exchange == null)
			return NotFound("Exchange not found");

		Db.Exchanges.Remove(exchange);
		await Db.SaveChangesAsync();

		return Success("Exchange deleted successfully");
	}

	[HttpGet("providers")]
	public IActionResult GetProviders()
	{
		var providers = Db.ExchangeProviders
			.AsNoTracking()
			.ToListAsync().GetAwaiter().GetResult()
			.Select(x =>
			{
				var data = x.GeneratePropertyMetadata();
				return new AMExchangeProvider
				{
					Id = x.Id,
					Name = x.Name,
					Title = x.Title,
					Properties = data
				};
			});

		return Success(providers);
	}
}