<template>
	<b-overlay
		class="min-h-300"
		:show="loading"
		no-fade
		:opacity="1"
		variant="dark"
		spinner-variant="primary"
	>
		<Bar
			:chart-data="{
				labels: walletNames,
				datasets: [
					{
						label: 'Positions',
						data: positionCounts,
						backgroundColor: wallets.map((x) => generateRandomColor()),
					},
					{
						label: 'Trades',
						data: tradeCounts,
						backgroundColor: wallets.map((x) => generateRandomColor()),
					},
				],
			}"
			:chart-options="{
				responsive: true,
				maintainAspectRatio: false,
			}"
			:styles="{ height: '300px' }"
		/>
	</b-overlay>
</template>
<script>
import Service from "@/services/dashboard";
import { Bar } from "vue-chartjs";
import {
	Chart as ChartJS,
	Title,
	Tooltip,
	Legend,
	BarElement,
	CategoryScale,
	LinearScale,
} from "chart.js";

ChartJS.register(
	Title,
	Tooltip,
	Legend,
	BarElement,
	CategoryScale,
	LinearScale
);

export default {
	data() {
		return {
			loading: false,
			wallets: [],
		};
	},
	async mounted() {
		await this.getHistogram();
	},
	computed: {
		tradeCounts: function () {
			return this.wallets.map((x) => x.tradeCount);
		},
		positionCounts: function () {
			return this.wallets.map((x) => x.positionCount);
		},
		walletNames: function () {
			return this.wallets.map((x) => x.label);
		},
	},
	methods: {
		async getHistogram() {
			this.loading = true;

			try {
				const response = await Service.getPositionHistogram();
				this.wallets = response.data.data;
			} catch (error) {
				this.$errorToast("Wallet Info", error.message);
			}

			this.loading = false;
		},
		generateRandomColor() {
			return "#" + Math.random().toString(16).slice(2, 8);
		},
	},
	components: {
		Bar,
	},
};
</script>
<style>
.min-h-300 {
	min-height: 300px;
}
</style>
