<template>
	<div class="row">
		<div class="col-md-3 px-2 pb-3">
			<div class="card-body">
				<p class="h5 mb-4">Max Profit</p>
				<b-overlay
					:show="loading"
					:opacity="1"
					variant="dark"
					spinner-variant="primary"
					no-fade
				>
					<span class="h1 m-0">{{ stats.maxProfit }}</span>
					<span class="ml-1 h5">USDT</span>
				</b-overlay>
			</div>
		</div>
		<div class="col-md-3 px-2 pb-3">
			<div class="card-body">
				<p class="h5 mb-4">Max Loss</p>
				<b-overlay
					:show="loading"
					:opacity="1"
					variant="dark"
					spinner-variant="primary"
					no-fade
				>
					<span class="h1 m-0">{{ stats.maxLoss }}</span>
					<span class="ml-1 h5">USDT</span>
				</b-overlay>
			</div>
		</div>
		<div class="col-md-3 px-2 pb-3">
			<div class="card-body">
				<p class="h5 mb-4">Wins</p>
				<b-overlay
					:show="loading"
					:opacity="1"
					variant="dark"
					spinner-variant="primary"
					no-fade
				>
					<span class="h1 m-0">{{ stats.winCount }}</span>
					<span class="ml-1 h5">X</span>
				</b-overlay>
			</div>
		</div>
		<div class="col-md-3 px-2 pb-3">
			<div class="card-body">
				<p class="h5 mb-4">Losses</p>
				<b-overlay
					:show="loading"
					:opacity="1"
					variant="dark"
					spinner-variant="primary"
					no-fade
				>
					<span class="h1 m-0">{{ stats.lossCount }}</span>
					<span class="ml-1 h5">X</span>
				</b-overlay>
			</div>
		</div>
	</div>
</template>
<script>
import Service from "@/services/dashboard";
export default {
	data() {
		return {
			loading: false,
			stats: {
				maxProfit: 0,
				maxLoss: 0,
				winCount: 0,
				lossCount: 0,
			},
		};
	},
	async mounted() {
		await this.getStats();
	},
	methods: {
		async getStats() {
			this.loading = true;

			try {
				const response = await Service.getImportantStats();
				if (response.data.data) this.stats = response.data.data;
			} catch (error) {
				this.$errorToast("Important Stats", error.message);
			}

			this.loading = false;
		},
	},
};
</script>
