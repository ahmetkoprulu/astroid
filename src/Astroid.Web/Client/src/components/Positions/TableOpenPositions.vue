<template>
	<v-table
		:columns="columns"
		:requestFunction="requestFunction"
		:refreshButton="false"
		ref="table"
	>
		<template #column-symbol="props">
			<div class="d-flex w-100">
				<span
					:class="{
						'mr-2': true,
						'bg-success': props.row.type == 1,
						'bg-danger': props.row.type == 2,
					}"
					style="width: 5px; height: 22px"
				/>
				<span>
					{{ props.row.symbol }}
				</span>
			</div>
		</template>
		<template #column-quantity="props">
			<div>
				{{ props.row.quantity }}
			</div>
			<div
				v-if="
					props.row.status !== 2 &&
					props.row.quantity != props.row.currentQuantity
				"
			>
				<span class="text-muted"> current: </span>
				{{ props.row.currentQuantity }}
			</div>
		</template>
		<template #column-entryPrice="props">
			<div>
				<span class="text-muted">entry: </span>{{ props.row.entryPrice }}
			</div>
			<div>
				<span class="text-muted">avg: </span>{{ props.row.averagePrice }}
			</div>
		</template>
		<template #column-actions="props">
			<v-dropdown class="pull-right">
				<v-dropdown-item @click="showHistory($event, props.row.orders)">
					<i class="fa-regular fa-file-lines mr-2" /> Show order history
				</v-dropdown-item>
				<v-dropdown-item @click="closePosition($event, props.row.id)">
					<i class="fa-solid fa-xmark mr-2" /> Close
				</v-dropdown-item>
			</v-dropdown>
		</template>
		<template #column-botLabel="props">
			<img :src="icons[props.row.exchangeProviderName]" height="20" />
			{{ props.row.exchangeLabel }}
		</template>
		<template #column-status="props">
			<b-badge variant="light" pill>{{
				$consts.POSITION_STATUS[props.row.status].title
			}}</b-badge>
			<OrderHistoryModal ref="orderHistoryModal" :show-status="false" />
		</template>
	</v-table>
</template>
<script>
import Service from "@/services/positions";
import { EXCHANGE_ICONS } from "@/core/consts";
import OrderHistoryModal from "@/components/Positions/PositionHistory.vue";

export default {
	data() {
		return {
			columns: {
				symbol: "Symbol",
				quantity: "Quantity",
				entryPrice: "Price",
				leverage: "Leverage",
				realizedPnl: "Est. RPnL",
				botLabel: "Bot",
				status: "Status",
				actions: " ",
			},
		};
	},
	computed: {
		icons() {
			return EXCHANGE_ICONS;
		},
	},
	methods: {
		async requestFunction(filters, sorts, currentPage) {
			return await Service.listOpen(filters, sorts, currentPage, 5);
		},
		showHistory(e, orders) {
			this.$refs.orderHistoryModal.show(orders);
		},
		closePosition(e, id) {
			this.$alert.remove(
				"Close The Position?",
				"You won't be able to undo it",
				async () => {
					try {
						const response = await Service.closePosition(id);
						if (!response.data.success) {
							this.$errorToast("Close Position", response.data.message);
							return;
						}

						this.$successToast(
							"Close Position",
							"Position closed successfully"
						);
						this.$refs.table.refresh();
					} catch (error) {
						this.$errorToast("Close Position", error.message);
					}
				},
				"<i class='fa-solid fa-xmark'></i> Yes, close it!"
			);
		},
		refresh() {
			this.$refs.table.refresh();
		},
	},
	components: { OrderHistoryModal },
};
</script>
