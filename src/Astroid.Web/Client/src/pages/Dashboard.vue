<template>
	<div>
		<page-header title="" />
		<ImportantStats />
		<div class="row">
			<div class="col-md-4 col-sm-5 col-lg-3 px-2 pb-3">
				<div class="card-body h-100">
					<p class="h5 mb-4">PnL</p>
					<TotalRealizedPnl />
				</div>
			</div>
			<div class="col-md-8 col-sm-7 col-lg-9 px-2 pb-3">
				<div class="card-body mb-3">
					<p class="h5 mb-4">Wallets</p>
					<WalletInfo />
				</div>
				<div class="card-body">
					<p class="h5 mb-4">Cumulative Est. Realized PnL</p>
					<CumulativeRealizedPnl />
				</div>
			</div>
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
import TotalRealizedPnl from "@/components/Dashboard/TotalRealizedPnl.vue";
import ImportantStats from "@/components/Dashboard/ImportantStats.vue";

export default {
	components: {
		WalletInfo,
		// PositionHistogram,
		CumulativeRealizedPnl,
		TotalRealizedPnl,
		ImportantStats,
	},
};
</script>

<style>
.wallet-info-contianer {
	min-height: 400px;
}
</style>
