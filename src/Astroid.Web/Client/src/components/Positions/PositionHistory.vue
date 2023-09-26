<template>
	<b-modal ref="modal" title="Order History" size="lg" ok-only @hidden="hidden">
		<span v-if="orders.length == 0">No orders</span>
		<table
			class="table table-condensed mt-0 w-100 d-block d-md-table flex-grow-1"
			v-else
		>
			<thead>
				<tr>
					<th>Type</th>
					<th>Quantity</th>
					<th>Trigger Price</th>
					<th>Close Position</th>
					<th>Status</th>
				</tr>
			</thead>
			<tbody>
				<tr v-for="order in orders" :key="order.id">
					<td>
						<b-badge pill variant="light">{{
							$consts.ORDER_TRIGGER_TYPES[order.triggerType].title
						}}</b-badge>
					</td>
					<td>{{ order.quantity }}</td>
					<td>{{ order.triggerPrice }}</td>
					<td>
						<b-badge pill :variant="order.closePosition ? 'success' : 'light'"
							>{{ order.closePosition ? "Yes" : "No" }}
						</b-badge>
					</td>
					<td>
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
	</b-modal>
</template>
<script>
export default {
	data() {
		return {
			orders: [],
		};
	},
	methods: {
		show(orders) {
			this.orders = orders;
			this.$refs.modal.show();
		},
		hide() {
			this.$refs.modal.hide();
		},
		hidden() {
			this.orders = [];
		},
	},
};
</script>
