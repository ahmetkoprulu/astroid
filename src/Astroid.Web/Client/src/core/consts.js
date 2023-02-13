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

export const AUDIT_TYPE_DESCRIPTIONS = {
	0: "Unknown",
	10: "Open Position",
	20: "Take Profit",
	30: "Stop Loss",
	40: "Close Position",
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

export default { PROPERTY_TYPES, AUDIT_TYPE_DESCRIPTIONS, POSITION_SIZE_TYPES, ORDER_MODE_TYPES, ORDER_ENTRY_TYPES, LIMIT_VALORIZATION_TYPES }