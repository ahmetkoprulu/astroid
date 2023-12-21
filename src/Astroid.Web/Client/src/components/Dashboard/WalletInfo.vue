<template>
	<b-overlay
		class="min-h-300"
		:show="loading"
		no-fade
		:opacity="1"
		variant="dark"
		spinner-variant="primary"
	>
		<div class="min-h-300" v-if="wallets.length > 0">
			<div class="row d-flex justify-content-between">
				<div class="col-md-4 col-sm-12">
					<div v-if="selectedWallet.isHealthy">
						<Doughnut
							:chart-data="{
								labels: selectedWallet.assets.map((x) => x.name),
								datasets: [
									{
										label: 'Balance',
										data: selectedWallet.assets.map((x) => x.balance),
										backgroundColor: selectedWallet.assets.map((x) =>
											generateRandomColor()
										),
									},
								],
							}"
							:chart-options="{
								responsive: true,
								maintainAspectRatio: false,
							}"
						/>
					</div>
					<div v-else>
						<div class="w-100 h-100 text-center">
							<span>{{ selectedWallet.errorMessage }}</span>
						</div>
					</div>
				</div>
				<div class="wallet-list col-md-8 col-sm-12 mt-4 mt-md-0">
					<div
						class="wallet-list-item p-2"
						:class="{ selected: selectedWallet.id == w.id }"
						v-for="w of wallets"
						@click="selectWallet($event, w)"
						:key="w.id"
					>
						<div class="d-flex justify-content-between">
							<span>
								<img
									class="mr-2"
									:src="$consts.EXCHANGE_ICONS[w.providerName]"
									height="20"
								/>{{ w.name }}</span
							>
							<span :class="w.isHealthy ? 'text-success' : 'text-danger'">
								{{ w.isHealthy ? "Healthy" : "Unhealthy" }}
							</span>
						</div>
					</div>
				</div>
			</div>
		</div>
		<div v-else>You do not have any wallet</div>
	</b-overlay>
</template>
<script>
import Service from "@/services/dashboard";
import { Doughnut } from "vue-chartjs";
import {
	Chart as ChartJS,
	Title,
	Tooltip,
	Legend,
	BarElement,
	ArcElement,
	CategoryScale,
	LinearScale,
} from "chart.js";

ChartJS.register(
	Title,
	Tooltip,
	Legend,
	ArcElement,
	BarElement,
	CategoryScale,
	LinearScale
);

export default {
	data() {
		return {
			loading: false,
			wallets: [],
			selectedWallet: null,
		};
	},
	async mounted() {
		await this.getWallets();
	},
	methods: {
		async getWallets() {
			this.loading = true;

			try {
				const response = await Service.getWalletInfo();
				this.wallets = response.data.data;
				if (this.wallets.length > 0) this.selectedWallet = this.wallets[0];
			} catch (error) {
				this.$errorToast("Wallet Info", error.message);
			}

			this.loading = false;
		},
		selectWallet(_, wallet) {
			this.selectedWallet = wallet;
		},
		generateRandomColor() {
			return "#" + Math.random().toString(16).slice(2, 8);
		},
	},
	components: {
		Doughnut,
	},
};
</script>
<style>
.min-h-300 {
	min-height: 300px;
}

.wallet-list {
	max-height: 300px;
	overflow-y: auto;
}

.wallet-list-item {
	cursor: pointer;
}

.wallet-list-item:hover {
	background-color: var(--md-sys-color-secondary-container-9);
	color: var(--md-sys-color-on-secondary-container);
}

.wallet-list-item.selected {
	background-color: var(--md-sys-color-surface-3);
}
</style>
