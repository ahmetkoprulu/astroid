<template>
	<v-table
		:columns="columns"
		:requestFunction="requestFunction"
		:refreshButton="false"
		ref="table"
	>
		<template #column-type="props">
			<b-badge pill variant="light">{{
				$consts.ORDER_TRIGGER_TYPES[props.row.triggerType].title
			}}</b-badge>
		</template>
		<template #column-quantity="props">
			{{
				props.row.quantityType == 1
					? `${props.row.quantity}%`
					: props.row.quantityType == 2
					? `${props.row.quantity} USDT`
					: props.row.quantityType == 3
					? `${props.row.quantity} ${props.row.symbol.replace("USDT", "")}`
					: props.row.quantity
			}}
			<div v-if="props.row.filledQuantity > 0">
				<span class="text-muted">filled: </span>
				{{ props.row.filledQuantity }}
			</div>
		</template>
		<template #column-realizedPnl="props">
			{{ props.row.realizedPnl }} USDT
		</template>
		<template #column-closePosition="props">
			<b-badge pill :variant="props.row.closePosition ? 'success' : 'light'">
				{{ props.row.closePosition ? "Yes" : "No" }}
			</b-badge>
		</template>
		<template #column-status="props">
			<b-badge
				pill
				:variant="
					props.row.status == 4 || props.row.status == 5 ? 'danger' : 'light'
				"
				>{{ $consts.ORDER_STATUS[props.row.status].title }}
			</b-badge>
		</template>
	</v-table>
</template>
<script>
import Service from "@/services/positions";

export default {
	data() {
		return {
			columns: {
				type: "Type",
				symbol: "Symbol",
				quantity: "Quantity",
				triggerPrice: "Trigger Price",
				realizedPnl: "Est. RPnL",
				closePosition: "Close Position",
				status: "Status",
			},
		};
	},
	methods: {
		async requestFunction(filters, sorts, currentPage) {
			return await Service.listTradeHistory(filters, sorts, currentPage, 5);
		},
	},
};
</script>
