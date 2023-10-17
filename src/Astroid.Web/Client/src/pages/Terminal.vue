<template>
	<div>
		<page-header title="Terminal" />

		<div class="row">
			<div
				class="col-md-12 col-sm-12 col-lg-9 col-xl-10 mb-4"
				style="height: 600px"
			>
				<TvChart :symbol="symbol" :key="symbol" />
			</div>
			<div class="col-sm-12 col-md-12 col-lg-3 col-xl-2 mb-4">
				<TradePanel
					@ticker-changed="onTickerChanged"
					@request-send="onRequestSent"
				/>
			</div>
		</div>
		<div class="row">
			<div class="col-md-12 col-sm-12">
				<PositionsTabs ref="positionTabs" />
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
