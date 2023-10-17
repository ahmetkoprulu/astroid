<template>
	<b-tabs content-class="mt-3 p-0" lazy>
		<b-tab title="Positions">
			<OpenPositionsTable ref="openPositionsTable" />
		</b-tab>
		<b-tab title="Trade History"><TradeHistoryTable /></b-tab>
		<b-tab title="Position History"><PositionHistoryTable /></b-tab>
	</b-tabs>
</template>
<script>
import OpenPositionsTable from "../Positions/TableOpenPositions.vue";
import PositionHistoryTable from "../Positions/TablePositionHistory.vue";
import TradeHistoryTable from "../Positions/TableTradeHistory.vue";

export default {
	methods: {
		refreshOpenOrders() {
			this.$refs.openPositionsTable.refresh();
		},
	},
	components: {
		OpenPositionsTable,
		PositionHistoryTable,
		TradeHistoryTable,
	},
};
</script>
<style>
.tabs .nav.nav-tabs {
	border: 0px !important;
}
.nav-tabs .nav-link {
	margin-bottom: -1px;
	background-color: transparent;
	border: 0px !important;
}
.nav-link.active {
	color: #2962ff !important;
}
</style>
