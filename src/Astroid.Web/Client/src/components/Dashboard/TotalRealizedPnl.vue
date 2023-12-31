<template>
	<b-overlay
		class="total-realized-pnl-container"
		:show="loading"
		:opacity="1"
		variant="dark"
		spinner-variant="primary"
		no-fade
	>
		<div>
			<span class="h1 m-0">{{ pnlHistory.lastTotalPnl }}</span>
			<span class="ml-1 h5">USD</span>
			<small class="ml-1">(Last 30 days)</small>
		</div>
		<div>
			<small
				:class="{
					'text-success': differenceInPercentage > 0,
					'text-danger': differenceInPercentage < 0,
				}"
			>
				{{ differenceInPercentage > 0 ? "▲" : "▼" }}
			</small>
			{{ differenceInPercentage }}% vs. prev. 30 days
		</div>
		<div>
			<p class="mt-4 h6">Recent Activities</p>
			<div class="light-table pr-2 scroll">
				<table class="naked-table table">
					<thead>
						<tr>
							<th scope="col">Order</th>
							<th scope="col">Market</th>
							<th scope="col">RPnL</th>
						</tr>
					</thead>
					<tbody>
						<tr
							class="border-posiiton"
							v-for="item of pnlHistory.items"
							:key="item.id"
						>
							<td
								:class="{
									'border-long': item.type == 1,
									'border-short': item.type == 2,
									'border-asset': item.providerType == 1,
								}"
							>
								{{ item.symbol }}
							</td>
							<td>
								<img
									class="mr-2"
									:src="$consts.EXCHANGE_ICONS[item.provider]"
									height="20"
								/>
							</td>
							<td>{{ item.realizedPnl }}</td>
						</tr>
					</tbody>
				</table>
			</div>
		</div>
	</b-overlay>
</template>
<script>
import Service from "@/services/dashboard";

export default {
	data() {
		return {
			loading: false,
			pnlHistory: {
				lastTotalPnl: 0,
				prevTotalPnl: 0,
				items: [],
			},
		};
	},
	computed: {
		differenceInPercentage() {
			if (this.pnlHistory.prevTotalPnl == 0)
				return (this.pnlHistory.lastTotalPnl * 100).toFixed(2);

			return (
				(this.pnlHistory.lastTotalPnl - this.pnlHistory.prevTotalPnl) *
				(100 / this.pnlHistory.prevTotalPnl)
			).toFixed(2);
		},
	},
	async mounted() {
		await this.getPnlHistory();
	},
	methods: {
		async getPnlHistory() {
			this.loading = true;

			try {
				const response = await Service.getPnlHistory();
				if (!response.data.data) return;

				this.pnlHistory = response.data.data;
			} catch (error) {
				this.$errorToast("PnL History", error.message);
			}

			this.loading = false;
		},
	},
};
</script>
<style>
.naked-table {
	width: 100%;
}

.total-realized-pnl-container {
	height: 710;
}

.naked-table.table th,
.naked-table.table td {
	border-top: 0px !important;
	padding: 8px 10px !important;
}

.naked-table.table {
	border-collapse: separate !important;
	border-spacing: 0 4px !important;
}

.light-table {
	max-height: 575px;
	overflow-y: hidden;
}

.naked-table .border-long {
	border-left: 5px solid var(--long) !important;
}

.naked-table .border-asset {
	border-left: 5px solid var(--neutral) !important;
}

.naked-table .border-short {
	border-left: 5px solid var(--short) !important;
	margin-bottom: 4px !important;
}
</style>
