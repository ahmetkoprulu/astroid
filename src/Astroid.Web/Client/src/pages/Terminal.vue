<template>
	<div>
		<page-header title="" />

		<div class="page-body row m-0">
			<div class="p-0 col-md-12 col-sm-12 col-lg-9" style="height: 600px">
				<div style="height: 600px">
					<TvChart :symbol="symbol" :key="symbol" />
				</div>
			</div>
			<div class="p-0 col-sm-12 col-md-12 col-lg-3 bl-1 bt-1 br-1">
				<div class="card-body h-100 border-radius-0">
					<TradePanel
						@ticker-changed="onTickerChanged"
						@request-send="onRequestSent"
					/>
				</div>
			</div>
		</div>
		<div class="bt-1">
			<div class="p-0 col-md-12 col-sm-12 bl-1 br-1 bb-1">
				<div class="card-body border-radius-0">
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
<style>
.bl-1 {
	border-left: 1px solid var(--md-sys-color-surface-9);
}

.bl-2 {
	border-left: 2px solid var(--md-sys-color-surface-9);
}

.br-1 {
	border-right: 1px solid var(--md-sys-color-surface-9);
}

.br-2 {
	border-right: 2px solid var(--md-sys-color-surface-9);
}

.bb-1 {
	border-bottom: 1px solid var(--md-sys-color-surface-9);
}

.bb-2 {
	border-bottom: 2px solid var(--md-sys-color-surface-9);
}

.bt-1 {
	border-top: 1px solid var(--md-sys-color-surface-9);
}

.bt-2 {
	border-top: 2px solid var(--md-sys-color-surface-9);
}

.b-1 {
	border: 1px solid var(--md-sys-color-surface-9);
}

.b-2 {
	border: 2px solid var(--md-sys-color-surface-9);
}

.border-radius-0 {
	border-top-left-radius: 0px !important;
	border-top-right-radius: 0px !important;
	border-bottom-right-radius: 0px !important;
	border-bottom-left-radius: 0px !important;
}
</style>
