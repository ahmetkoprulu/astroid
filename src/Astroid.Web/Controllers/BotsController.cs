using Microsoft.AspNetCore.Mvc;
using Astroid.Entity;
using Astroid.Web.Models;
using Astroid.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Astroid.Providers;
using Astroid.Web.Helpers;

namespace Astroid.Web;

public class BotsController : SecureController
{
	public BotsController(AstroidDb db) : base(db) { }

	[HttpPost("list")]
	public async Task<IActionResult> List([FromBody] MPViewDataList<ADBot> model)
	{
		model ??= new MPViewDataList<ADBot>();

		model = await Db.Bots
			.AsNoTracking()
			.OrderByDescending(x => x.CreatedDate)
			.ViewDataListAsync<ADBot>(model);

		return Success(model.ForJson(x => new AMBot
		{
			Id = x.Id,
			Label = x.Label,
			Description = x.Description,
		}));
	}

	[HttpGet("{id}")]
	public async Task<IActionResult> Get(Guid id)
	{
		if (id == Guid.Empty)
			return BadRequest("Invalid exchange id");

		var bot = await Db.Bots
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id == id);
		if (bot == null)
			return NotFound("Exchange not found");

		return Success(new AMBot
		{
			Id = bot.Id,
			Label = bot.Label,
			Description = bot.Description,
			ExchangeId = bot.ExchangeId,
			OrderType = bot.OrderType,
			PositionSizeType = bot.PositionSizeType,
			PositionSize = bot.PositionSize,
			IsTakePofitEnabled = bot.IsTakePofitEnabled,
			ProfitActivation = bot.ProfitActivation,
			IsStopLossEnabled = bot.IsStopLossEnabled,
			StopLossActivation = bot.StopLossActivation,
			Key = bot.Key,
			IsEnabled = bot.IsEnabled
		});
	}

	[HttpPost("save")]
	public async Task<IActionResult> Save([FromBody] AMBot model)
	{
		var isNameExists = await Db.Bots.AnyAsync(x => x.Label == model.Label && x.Id != model.Id);
		if (isNameExists) BadRequest("Exchange name already exists");

		if (model.Id == Guid.Empty)
		{
			var bot = new ADBot
			{
				Id = Guid.NewGuid(),
				Label = model.Label,
				Description = model.Description,
				ExchangeId = model.ExchangeId,
				OrderType = model.OrderType,
				PositionSizeType = model.PositionSizeType,
				PositionSize = model.PositionSize,
				IsTakePofitEnabled = model.IsTakePofitEnabled,
				ProfitActivation = model.ProfitActivation,
				IsStopLossEnabled = model.IsStopLossEnabled,
				StopLossActivation = model.StopLossActivation,
				Key = model.Key,
				CreatedDate = DateTime.Now,
				UserId = CurrentUser.Id
			};

			await Db.Bots.AddAsync(bot);
		}
		else
		{
			var bot = await Db.Bots.FirstOrDefaultAsync(x => x.Id == model.Id);
			if (bot == null)
				return NotFound("Exchange not found");

			bot.Label = model.Label;
			bot.Description = model.Description;
			bot.ExchangeId = model.ExchangeId;
			bot.OrderType = model.OrderType;
			bot.PositionSizeType = model.PositionSizeType;
			bot.PositionSize = model.PositionSize;
			bot.IsTakePofitEnabled = model.IsTakePofitEnabled;
			bot.ProfitActivation = model.ProfitActivation;
			bot.IsStopLossEnabled = model.IsStopLossEnabled;
			bot.StopLossActivation = model.StopLossActivation;
			bot.Key = model.Key;
			bot.IsEnabled = model.IsEnabled;
			bot.ModifiedDate = DateTime.Now;
		}

		await Db.SaveChangesAsync();

		return Success("Exchange saved successfully");
	}

	[HttpDelete("{id}")]
	public async Task<IActionResult> Delete(Guid id)
	{
		if (id == Guid.Empty)
			return BadRequest("Invalid exchange id");

		var bot = await Db.Bots.FirstOrDefaultAsync(x => x.Id == id);
		if (bot == null)
			return NotFound("Exchange not found");

		Db.Bots.Remove(bot);
		await Db.SaveChangesAsync();

		return Success("Exchange deleted successfully");
	}
}