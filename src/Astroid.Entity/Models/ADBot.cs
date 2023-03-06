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
	public OrderEntryType OrderType { get; set; }
	public OrderMode OrderMode { get; set; }
	public PositionSizeType PositionSizeType { get; set; }
	public decimal? PositionSize { get; set; }
	public bool IsPositionSizeExpandable { get; set; }
	[NotMapped]
	public LimitSettings LimitSettings
	{
		get => this.GetAs<LimitSettings>(LimitSettingsJson);
		set => LimitSettingsJson = this.SetAs(value);
	}
	[Column(nameof(LimitSettings))]
	public string LimitSettingsJson { get; set; }
	public bool IsTakePofitEnabled { get; set; }
	public decimal? ProfitActivation { get; set; }
	public bool IsStopLossEnabled { get; set; }
	public decimal? StopLossActivation { get; set; }
	public string Key { get; set; }
	public bool IsEnabled { get; set; }
	public Guid UserId { get; set; }
	public DateTime CreatedDate { get; set; }
	public DateTime ModifiedDate { get; set; }
}
