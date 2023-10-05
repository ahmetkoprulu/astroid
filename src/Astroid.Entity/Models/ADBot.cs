using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Astroid.Core;
using Astroid.Entity.Extentions;

namespace Astroid.Entity;

[Table("Bots")]
public class ADBot : IEntity
{
	[Key]
	public Guid Id { get; set; }
	public string Label { get; set; }
	public string? Description { get; set; }
	public Guid ExchangeId { get; set; }
	// public SignalSourceType SignalSourceType { get; set; }
	public OrderEntryType OrderType { get; set; } = OrderEntryType.Market;
	public OrderMode OrderMode { get; set; } = OrderMode.TwoWay;
	public PositionSizeType PositionSizeType { get; set; } = PositionSizeType.FixedInUsd;
	public decimal? PositionSize { get; set; }
	public bool IsPositionSizeExpandable { get; set; }
	[Column(nameof(LimitSettings))]
	public string LimitSettingsJson { get; set; }
	public bool IsPyramidingEnabled { get; set; }
	[Column(nameof(PyramidingSettings))]
	public string PyramidingSettingsJson { get; set; }
	public bool IsTakePofitEnabled { get; set; }
	[Column(nameof(TakeProfitSettings))]
	public string TakeProfitSettingsJson { get; set; }
	public bool IsStopLossEnabled { get; set; }
	public StopLossType StopLossType { get; set; } = StopLossType.Fixed;
	public decimal? StopLossPrice { get; set; }
	public decimal? StopLossCallbackRate { get; set; }
	public string Key { get; set; }
	public bool IsEnabled { get; set; }
	public BotState State { get; set; }
	public Guid UserId { get; set; }
	public DateTime CreatedDate { get; set; }
	public DateTime ModifiedDate { get; set; }
	public Guid? ManagedBy { get; set; }

	[NotMapped]
	public PyramidingSettings PyramidingSettings
	{
		get => this.GetAs<PyramidingSettings>(PyramidingSettingsJson);
		set => PyramidingSettingsJson = this.SetAs(value);
	}

	[NotMapped]
	public TakeProfitSettings TakeProfitSettings
	{
		get => this.GetAs<TakeProfitSettings>(TakeProfitSettingsJson);
		set => TakeProfitSettingsJson = this.SetAs(value);
	}

	[NotMapped]
	public LimitSettings LimitSettings
	{
		get => this.GetAs<LimitSettings>(LimitSettingsJson);
		set => LimitSettingsJson = this.SetAs(value);
	}
}

public class PyramidingSettings
{
	public List<PyramidTarget> Targets { get; set; } = new();
}

public class TakeProfitSettings
{
	public CalculationBase CalculationBase { get; set; } = CalculationBase.LastPrice;
	public PriceBase PriceBase { get; set; } = PriceBase.LastPrice;
	public List<TakeProfitTarget> Targets { get; set; } = new();
}

public enum BotState : short
{
	Unknown = 0,
	Preparing = 1,
	Enabled = 2,
	Disabled = 3,
	Stopping = 4
}

public enum CalculationBase
{
	Unknown = 0,
	EntryPrice = 1,
	AveragePrice = 2,
	LastPrice = 3,
}

public enum PriceBase : short
{
	Unknown = 0,
	LastPrice = 1,
	MarkPrice = 2,
	IndexPrice = 3
}
