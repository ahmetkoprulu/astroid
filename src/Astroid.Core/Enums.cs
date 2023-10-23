using System.ComponentModel;

namespace Astroid.Core;

public enum PositionType : short
{
	Unknown = 0,
	Long = 1,
	Short = 2,
	Both = 3
}

public enum OrderType : short
{
	Unknown = 0,
	Buy = 1,
	Sell = 2
}

public enum SignalSourceType : short
{
	Unknown = 0,
	TradingView = 1,
	Custom = 2
}

public enum OrderEntryType : short
{
	Unknown = 0,
	Market = 1,
	Limit = 2
}

public enum OrderMode : short
{
	Unknown = 0,
	OneWay = 1,
	TwoWay = 2,
	Swing = 3
}

public enum PositionSizeType : short
{
	Unknown = 0,
	Ratio = 1,
	FixedInUsd = 2,
	FixedInAsset = 3
}

public enum MarginType : short
{
	Isolated = 0,
	Cross = 1
}

public enum AuditType : short
{
	Unknown = 0,
	OrderRequest = 1,
	OpenOrderPlaced = 10,
	TakeProfitOrderPlaced = 20,
	StopLossOrderPlaced = 30,
	CloseOrderPlaced = 40,
	ChangeMarginType = 41,
	UnhandledException = 50,
}

public enum ChannelType : short
{
	Mail = 0,
	Sms = 1,
	Telegram = 2
}

public enum NotificationStatus : short
{
	Pending = 1,
	Processing = 2,
	Sent = 3,
	Failed = 4,
	WaitingForService = 5,
	Cancelled = 6,
	Expired = 7
}

public enum DatabaseProvider
{
	Unknown = 0,
	MsSql = 1,
	PostgreSql = 2,
	MySql = 3,
	InMemory = 9
}

public enum ValorizationType
{
	Unknown = 0,
	LastPrice = 1,
	OrderBook = 2,
}

public enum PropertyTypes : short
{
	UnTyped = 0,
	Text = 1,
	Number = 2,
	Boolean = 15,
	Date = 3,
	DateTime = 4,
	Dropdown = 5,
	RadioButton = 6,
	Checklist = 7,
	Image = 10,
	Money = 12,
	Decimal = 13,
	Time = 14,
	Json = 20,
	KeyValue = 22,
	DynamicSettings = 23
}

public enum PositionStatus
{
	Unknown = 0,
	Open = 1,
	Closed = 2,
	Requested = 3,
	Rejected = 4
}

public enum OrderTriggerType
{
	[Description("Unknown")]
	Unknown = 0,
	[Description("Stop Loss")]
	StopLoss = 1,
	[Description("Take Profit")]
	TakeProfit = 2,
	[Description("Pyramiding")]
	Pyramiding = 3,
	[Description("Sell")]
	Sell = 4,
	[Description("Buy")]
	Buy = 5
}

public enum OrderStatus
{
	Unknown = 0,
	Open = 1,
	Triggered = 2,
	Filled = 3,
	Rejected = 4,
	Cancelled = 5,
	Expired = 6,
}

public enum StopLossType
{
	Unknown = 0,
	Fixed,
	Trailing,
	TrailingProfit
}

public enum OrderConditionType : short
{
	Unknown = 0,
	Increasing = 1,
	Decreasing = 2,
	Immediate = 3
}
