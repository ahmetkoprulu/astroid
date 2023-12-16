using Astroid.Core;
using Astroid.Entity;
using Newtonsoft.Json;

namespace Astroid.Providers;

public record AMProviderResult
{
	public bool Success { get; set; }
	public string? Message { get; set; }
	public List<ADAudit> Audits { get; set; } = new();
	public string? CorrelationId { get; set; }
	public ADPosition Position { get; set; }
	public ADOrder Order { get; set; }

	public AMProviderResult(ADPosition position, ADOrder order)
	{
		Position = position;
		Order = order;
		CorrelationId = Guid.NewGuid().ToString();
	}

	public static AMProviderResult Create(ADOrder order) => new(order.Position, order);

	public static AMProviderResult Create(ADPosition position) => new(position, null);

	public static AMProviderResult Create() => new(null, null);

	public AMProviderResult WithSuccess()
	{
		Success = true;

		return this;
	}

	public AMProviderResult WithMessage(string message)
	{
		Message = message;

		return this;
	}

	public AMProviderResult AddAudit(AuditType type, string description, string? correlationId = null, string? data = null)
	{
		var audit = new ADAudit
		{
			Id = Guid.NewGuid(),
			UserId = Order.UserId,
			ActorId = Order.BotId,
			TargetId = Order.Id,
			Type = type,
			Description = description,
			CorrelationId = Position.Id.ToString(),
			Data = data,
			CreatedDate = DateTime.UtcNow,
		};
		Audits.Add(audit);

		return this;
	}

	public void SaveAudits(AstroidDb db)
	{
		if (Audits.Count == 0) return;

		db.Audits.AddRange(Audits);
	}

	public bool ValidateOpenRequest(ADBot bot)
	{
		if (Position.BotId != bot.Id)
		{
			AddAudit(AuditType.OpenOrderPlaced, $"The position for {Order.Symbol} - {Position.Type} already exists and managed by.", data: JsonConvert.SerializeObject(new { Order.Symbol, Order.Position.Type }))
				.WithMessage("The position not managed by this bot.");

			return false;
		}

		if (Position.Status != PositionStatus.Requested && !bot.IsPositionSizeExpandable)
		{
			AddAudit(AuditType.OpenOrderPlaced, $"Position size is not expandable", data: JsonConvert.SerializeObject(new { Order.Symbol, Order.Position.Type }))
				.WithMessage($"Position size is not expandable");

			return false;
		}

		if (bot.OrderMode == OrderMode.OneWay)
		{
			AddAudit(AuditType.OpenOrderPlaced, $"Position already exists for {Order.Symbol} - {Position.Type}", data: JsonConvert.SerializeObject(new { Order.Symbol, Order.Position.Type }))
				.WithMessage($"Position already exists for {Order.Symbol} - {Position.Type}");

			return false;
		}

		return true;
	}

	public bool ValidateStopLoss(ADBot bot)
	{
		if (bot.StopLossSettings.Price <= 0)
		{
			AddAudit(AuditType.StopLossOrderPlaced, $"Failed placing stop loss order: Stop loss price is zero.", CorrelationId, JsonConvert.SerializeObject(new { Order.Symbol }));

			return false;
		}

		return true;
	}

	public bool ValidateTakeProfit(ADBot bot)
	{
		if (bot.TakeProfitSettings.Targets.Count == 0)
		{
			AddAudit(AuditType.TakeProfitOrderPlaced, $"Failed placing take profit order: No target to place order.", CorrelationId, JsonConvert.SerializeObject(new { Order.Symbol }));

			return false;
		}

		var targets = bot.TakeProfitSettings.Targets;
		if (targets.Any(x => !(x.Quantity > 0) || !(x.Target > 0)))
		{
			AddAudit(AuditType.TakeProfitOrderPlaced, $"Failed placing take profit order: Target(s) share or price value is empty/zero.", CorrelationId, JsonConvert.SerializeObject(new { Order.Symbol }));

			return false;
		}

		return true;
	}

	public bool ValidatePyramiding(ADBot bot)
	{
		if (bot.PyramidingSettings.Targets.Count == 0)
		{
			AddAudit(AuditType.OpenOrderPlaced, $"Failed placing pyramiding order: No target to place order.", CorrelationId, JsonConvert.SerializeObject(new { Order.Symbol }));

			return false;
		}

		var targets = bot.PyramidingSettings.Targets;
		if (targets.Any(x => !(x.Quantity > 0) || !(x.Target == 0)))
		{
			AddAudit(AuditType.OpenOrderPlaced, $"Failed placing pyramiding order: Target(s) share or price value is empty/zero.", CorrelationId, JsonConvert.SerializeObject(new { Order.Symbol }));

			return false;
		}

		return true;
	}

	public bool ValidateCloseRequest(ADBot bot)
	{
		if (Position.BotId != bot.Id)
		{
			AddAudit(AuditType.OpenOrderPlaced, $"The position for {Order.Symbol} - {Position.Type} managed by another bot.", data: JsonConvert.SerializeObject(new { Order.Symbol, Order.Position.Type }))
				.WithMessage("The position not managed by this bot.");
			return false;
		}

		return true;
	}
}
