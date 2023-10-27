<template>
	<div class="chart w-100 h-100">
		<div :id="container_id" class="w-100 h-100" v-if="symbol" />
		<div
			class="w-100 h-100 d-flex"
			style="justify-content: center; align-content: center; flex-wrap: wrap"
			v-else
		>
			Select a symbol to view chart
		</div>
	</div>
</template>

<script>
const SCRIPT_ID = "tradingview-widget-script";
const CONTAINER_ID = "chart";

export default {
	name: "VueTradingView",
	props: {
		symbol: {
			type: String,
		},
	},
	data() {
		return {
			container_id: CONTAINER_ID,
			optionsData: {
				autosize: true,
				interval: "D",
				timezone: "Etc/UTC",
				theme: "dark",
				style: "1",
				locale: "en",
				enable_publishing: false,
				hide_legend: false,
				hide_side_toolbar: true,
				save_image: false,
				hide_volume: true,
			},
			exchangeLabel: {
				"binance-usd-futures": "BINANCE",
			},
		};
	},
	computed: {
		opt() {
			return { symbol: this.symbol, ...this.optionsData };
		},
	},
	methods: {
		canUseDOM() {
			return (
				typeof window !== "undefined" &&
				window.document &&
				window.document.createElement
			);
		},
		getScriptElement() {
			return document.getElementById(SCRIPT_ID);
		},
		updateOnloadListener(onload) {
			const script = this.getScriptElement();
			const oldOnload = script.onload;
			return (script.onload = () => {
				oldOnload();
				onload();
			});
		},
		scriptExists() {
			return this.getScriptElement() !== null;
		},
		appendScript(onload) {
			if (!this.canUseDOM()) {
				onload();
				return;
			}

			if (this.scriptExists()) {
				if (typeof TradingView === "undefined") {
					this.updateOnloadListener(onload);
					return;
				}
				onload();
				return;
			}
			const script = document.createElement("script");
			script.id = SCRIPT_ID;
			script.type = "text/javascript";
			script.async = true;
			script.src = "https://s3.tradingview.com/tv.js";
			script.onload = onload;
			document.getElementsByTagName("head")[0].appendChild(script);
		},
		initWidget() {
			if (typeof TradingView === "undefined") {
				return;
			}
			this.opt.theme = this.$theme;
			let docStyle = getComputedStyle(document.body);
			let backkgroundColor = docStyle.getPropertyValue(
				"--md-sys-color-background"
			);
			let gridColor = docStyle.getPropertyValue("--md-sys-color-surface-1");
			this.opt.backgroundColor = backkgroundColor;
			this.opt.gridColor = gridColor;

			new window.TradingView.widget(
				Object.assign({ container_id: this.container_id }, this.opt)
			);
		},
	},
	mounted() {
		if (!this.symbol) return;
		this.appendScript(this.initWidget);
	},
};
</script>
<style scoped>
.chart {
	/* border-radius: 0.375rem; */
}
</style>
