namespace Astroid.Core;

public enum PositionType : short
{
	Unknown = 0,
	Long = 1,
	Short = 2
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

public enum AuditType : short
{
	Unknown = 0,
	OpenOrderPlaced = 10,
	TakeProfitOrderPlaced = 20,
	StopLossOrderPlaced = 30,
	CloseOrderPlaced = 40,
	UnhandledException = 50,
}

public enum DatabaseProvider
{
	Unknown = 0,
	MsSql = 1,
	PostgreSql = 2,
	MySql = 3,
	InMemory = 9
}

public enum DeviationType
{
	LastPrice = 0,
	AskPrice = 1,
	BidPrice = 2,
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
