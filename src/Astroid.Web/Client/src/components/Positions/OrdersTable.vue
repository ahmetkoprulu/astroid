<template>
	<table
		class="table table-condensed mt-0 w-100 d-block d-md-table flex-grow-1"
	>
		<thead>
			<tr>
				<th>Type</th>
				<th>Quantity</th>
				<th>Trigger Price</th>
				<th>Close Position</th>
				<th v-if="showStatus">Status</th>
			</tr>
		</thead>
		<tbody>
			<tr v-for="order in orders" :key="order.id">
				<td>
					<b-badge pill variant="light">{{
						$consts.ORDER_TRIGGER_TYPES[order.triggerType].title
					}}</b-badge>
				</td>
				<td>
					{{
						order.quantityType == 1
							? `${order.quantity}%`
							: order.quantityType == 2
							? `${order.quantity} USDT`
							: order.quantityType == 3
							? `${order.quantity} ${order.symbol.replace("USDT", "")}`
							: order.quantity
					}}
					<div v-if="order.filledQuantity > 0">
						<span class="text-muted">filled: </span>
						{{ order.filledQuantity }}
					</div>
				</td>
				<td>{{ order.triggerPrice }}</td>
				<td>
					<b-badge pill :variant="order.closePosition ? 'success' : 'light'"
						>{{ order.closePosition ? "Yes" : "No" }}
					</b-badge>
				</td>
				<td v-if="showStatus">
					<b-badge
						pill
						:variant="
							order.status == 4 || order.status == 5 ? 'danger' : 'light'
						"
						>{{ $consts.ORDER_STATUS[order.status].title }}</b-badge
					>
				</td>
			</tr>
		</tbody>
	</table>
</template>
<script>
export default {
	props: {
		orders: {
			type: Array,
			default: () => [],
		},
		showStatus: {
			type: Boolean,
			default: true,
		},
	},
};
</script>
