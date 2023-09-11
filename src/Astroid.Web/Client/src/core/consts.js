export const PROPERTY_TYPES = {
	UnTyped: 0,
	Text: 1,
	Number: 2,
	Boolean: 15,
	Date: 3,
	DateTime: 4,
	Dropdown: 5,
	RadioButton: 6,
	Checklist: 7,
	Image: 10,
	Money: 12,
	Decimal: 13,
	Time: 14,
	Json: 20,
	KeyValue: 22,
	DynamicSettings: 23
}

export const EXCHANGE_ICONS = {
	"binance-usd-futures": "https://upload.wikimedia.org/wikipedia/commons/e/e8/Binance_Logo.svg",
}

export const AUDIT_TYPE_DESCRIPTIONS = {
	0: "Unknown",
	1: "Order Request",
	10: "Open Position",
	20: "Take Profit",
	30: "Stop Loss",
	40: "Close Position",
	41: "Change Margin Type",
	50: "Exception",
}

export const POSITION_SIZE_TYPES = {
	1: "Percentage",
	2: "Fixed In USD",
	3: "Fixed In Asset",
}

export const ORDER_MODE_TYPES = {
	0: "Unknown",
	1: "One Way",
	2: "Two Way",
	3: "Swing"
}

export const ORDER_ENTRY_TYPES = {
	0: "Unknown",
	1: "Market",
	2: "Limit",
}

export const LIMIT_VALORIZATION_TYPES = {
	1: "Last Price",
	2: "Order Book"
}

export const LIMIT_ORDER_BOOK_COMPUTATION_METHODS = {
	1: { id: 1, title: "Standard Deviation", icon: "fa fa-calculator" },
	2: { id: 2, title: "Code", icon: "fa fa-code" },
	// 2: { title: "Fixed Spread", icon: "fa fa-arrows-h" },
	// 3: { title: "Percentage Spread", icon: "fa fa-percent" },
}

export const STOP_LOSS_TYPE = {
	1: { id: 1, title: "Fixed", icon: "fa fa-arrows-down-to-line" },
	2: { id: 2, title: "Trailing", icon: "fa fa-stairs" },
	3: { id: 3, title: "Trailing Profit", icon: "fa fa-bars" }
}

export const ORDER_TRIGGER_TYPES = {
	1: { id: 1, title: "Stop Loss", icon: "fa fa-dollar-sign" },
	2: { id: 2, title: "Take Profit", icon: "fa fa-dollar-sign" },
	3: { id: 3, title: "Pyramiding", icon: "fa fa-dollar-sign" },
}

export const ORDER_STATUS = {
	1: { id: 1, title: "Open", icon: "fa fa-dollar-sign" },
	2: { id: 2, title: "Triggered", icon: "fa fa-dollar-sign" },
	3: { id: 3, title: "Filled", icon: "fa fa-dollar-sign" },
	4: { id: 4, title: "Rejected", icon: "fa fa-dollar-sign" },
	5: { id: 5, title: "Cancelled", icon: "fa fa-dollar-sign" },
	6: { id: 6, title: "Expired", icon: "fa fa-dollar-sign" },
}

export default { PROPERTY_TYPES, EXCHANGE_ICONS, AUDIT_TYPE_DESCRIPTIONS, POSITION_SIZE_TYPES, ORDER_MODE_TYPES, ORDER_ENTRY_TYPES, LIMIT_VALORIZATION_TYPES, LIMIT_ORDER_BOOK_COMPUTATION_METHODS, STOP_LOSS_TYPE, ORDER_TRIGGER_TYPES, ORDER_STATUS }