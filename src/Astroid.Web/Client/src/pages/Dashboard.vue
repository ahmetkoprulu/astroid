<template>
	<div>
		<page-header title="Dashboard" />

		<div class="col-md-7 card-body p-4 mb-4 wallet-info-contianer">
			<p class="h5 mb-4">Wallets</p>
			<WalletInfo />
		</div>
		<div class="col-md-7 card-body p-4 mb-4">
			<p class="h5 mb-4">Cumulative Est. Realized PnL</p>
			<CumulativeRealizedPnl />
		</div>
		<!-- <div class="card-body p-4 mb-4">
			<p class="h5 mb-4">Position Activity</p>
		</div>
		<div class="card-body p-4 mb-4">
			<p class="h5 mb-4">Wallet Activity</p>
			<PositionHistogram />
		</div> -->
	</div>
</template>

<script>
import WalletInfo from "@/components/Dashboard/WalletInfo.vue";
// import PositionHistogram from "@/components/Dashboard/PositionHistogram.vue";
import CumulativeRealizedPnl from "@/components/Dashboard/CumulativeRealizedPnl.vue";

export default {
	components: {
		WalletInfo,
		// PositionHistogram,
		CumulativeRealizedPnl,
	},
};
</script>

<style>
.wallet-info-contianer {
	min-height: 400px;
}
</style>
