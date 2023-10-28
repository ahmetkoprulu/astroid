<template>
	<div>
		<page-header title="Terminal" />

		<div class="page-body row">
			<div
				class="pr-md-2 col-md-12 col-sm-12 col-lg-9 mb-4"
				style="height: 600px"
			>
				<div style="height: 600px">
					<TvChart :symbol="symbol" :key="symbol" />
				</div>
			</div>
			<div class="px-3 col-sm-12 col-md-12 col-lg-3 mb-4">
				<div class="card-body h-100">
					<TradePanel
						@ticker-changed="onTickerChanged"
						@request-send="onRequestSent"
					/>
				</div>
			</div>
		</div>
		<div class="row">
			<div class="px-3 p-0 col-md-12 col-sm-12">
				<div class="card-body">
					<PositionsTabs ref="positionTabs" />
				</div>
			</div>
		</div>
	</div>
</template>
<script>
import TvChart from "../components/Terminal/TvChart.vue";
import TradePanel from "../components/Terminal/TradePanel.vue";
import PositionsTabs from "../components/Terminal/PositionsTabs.vue";

export default {
	data() {
		return {
			symbol: null,
		};
	},
	computed: {},
	methods: {
		onTickerChanged(ticker) {
			this.symbol = ticker;
		},
		onRequestSent() {
			this.$refs.positionTabs.refreshOpenOrders();
		},
	},
	components: {
		TvChart,
		TradePanel,
		PositionsTabs,
	},
};
</script>
