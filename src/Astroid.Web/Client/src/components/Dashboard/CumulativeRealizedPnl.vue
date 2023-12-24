<template>
	<b-overlay
		class="min-h-300"
		:show="loading"
		no-fade
		:opacity="1"
		variant="dark"
		spinner-variant="primary"
	>
		<LineChart
			:chart-data="{
				labels: Array.from(Array(30).keys()).map((x) => x + 1),
				datasets: [
					{
						label: 'Last 30',
						data: getThirtyRandomNumber(),
						borderColor: lastColor,
						tension: 0.3,
						fill: true,
					},
					{
						label: 'Prev. 30',
						data: getThirtyRandomNumber(),
						borderColor: prevColor,
						tension: 0.3,
					},
				],
			}"
			:chart-options="{
				responsive: true,
				maintainAspectRatio: false,
				scales: {
					x: {
						grid: {
							color: outlineColor,
						},
						border: {
							color: borderColor,
						},
						ticks: {
							color: tickColor,
						},
					},
					y: {
						grid: {
							color: outlineColor,
						},
						border: {
							color: borderColor,
						},
						ticks: {
							color: tickColor,
						},
					},
				},
			}"
			:styles="{ height: '300px' }"
		/>
	</b-overlay>
</template>
<script>
import Service from "@/services/dashboard";
import { Line as LineChart } from "vue-chartjs";
import {
	Chart as ChartJS,
	Title,
	Tooltip,
	Legend,
	LineElement,
	LinearScale,
	CategoryScale,
	PointElement,
} from "chart.js";

ChartJS.register(
	Title,
	Tooltip,
	Legend,
	LineElement,
	LinearScale,
	CategoryScale,
	PointElement
);

export default {
	data() {
		return {
			loading: false,
			data: {},
		};
	},
	async mounted() {
		await this.getCumulativePnl();
	},
	computed: {
		last30DaysPnl: function () {
			if (!this.data.last30Days) return [];

			return this.data.last30Days.map((x) => x.cumulativePnl);
		},
		prev30DaysPnl: function () {
			if (!this.data.previous30Days) return [];

			return this.data.previous30Days.map((x) => x.cumulativePnl);
		},
		lastColor() {
			let docStyle = getComputedStyle(document.body);
			let backkgroundColor = docStyle.getPropertyValue(
				"--md-sys-color-primary"
			);

			return backkgroundColor;
		},
		prevColor() {
			let docStyle = getComputedStyle(document.body);
			let backkgroundColor = docStyle.getPropertyValue(
				"--md-sys-color-secondary-container"
			);

			return backkgroundColor;
		},
		outlineColor() {
			let docStyle = getComputedStyle(document.body);
			let backkgroundColor = docStyle.getPropertyValue(
				"--md-sys-color-outline-9"
			);

			return backkgroundColor;
		},
		borderColor() {
			let docStyle = getComputedStyle(document.body);
			let backkgroundColor = docStyle.getPropertyValue(
				"--md-sys-color-outline"
			);

			return backkgroundColor;
		},
		tickColor() {
			let docStyle = getComputedStyle(document.body);
			let backkgroundColor = docStyle.getPropertyValue(
				"--md-sys-color-outline"
			);

			return backkgroundColor;
		},
	},
	methods: {
		async getCumulativePnl() {
			this.loading = true;

			try {
				const response = await Service.getCumulativeProfit();
				this.data = response.data.data;
			} catch (error) {
				this.$errorToast("Wallet Info", error.message);
			}

			this.loading = false;
		},
		getThirtyRandomNumber() {
			return Array.from(Array(30).keys()).map(() =>
				Math.floor(Math.random() * 255)
			);
		},
		generateRandomColor() {
			return "#" + Math.random().toString(16).slice(2, 8);
		},
	},
	components: {
		LineChart,
	},
};
</script>
<style>
.min-h-300 {
	min-height: 300px;
}
</style>
