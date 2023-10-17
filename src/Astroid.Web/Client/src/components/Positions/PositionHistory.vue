<template>
	<b-modal ref="modal" title="Order History" size="lg" ok-only @hidden="hidden">
		<span v-if="orders.length == 0">No orders</span>
		<OrderHistoryTable v-else :orders="orders" :show-status="showStatus" />
	</b-modal>
</template>
<script>
import OrderHistoryTable from "./OrdersTable.vue";

export default {
	props: {
		showStatus: {
			type: Boolean,
			default: true,
		},
	},
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
	components: {
		OrderHistoryTable,
	},
};
</script>
