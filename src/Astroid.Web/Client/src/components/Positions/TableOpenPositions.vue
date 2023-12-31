<template>
	<v-table
		:columns="columns"
		:requestFunction="requestFunction"
		:refreshButton="false"
		table-class="naked-table text-nowrap"
		ref="table"
	>
		<template #row-base="props">
			<tr class="border-posiiton">
				<td
					class="p-0"
					:class="{
						'border-long': props.row.exchange.providerType == 1,
						'border-short': props.row.exchange.providerType == 2,
						'border-asset': props.row.exchange.providerType == 1,
					}"
				>
					<img
						:src="icons[props.row.exchange.providerName]"
						class="mx-2"
						height="20"
					/>
					<span class="mr-2">
						{{ props.row.symbol }}
					</span>
					<b-badge variant="light" pill> {{ props.row.leverage }}x </b-badge>
				</td>
				<td>
					{{ props.row.currentQuantity }}
				</td>
				<td>
					{{ props.row.averagePrice }}
				</td>
				<td>
					{{ props.row.weightedEntryPrice }}
				</td>
				<td>{{ props.row.realizedPnl }}</td>
				<!-- <td>{{ props.row.botLabel }}</td> -->
				<td>
					<v-dropdown size="xs" class="pull-right">
						<v-dropdown-item @click="showHistory($event, props.row.orders)">
							<i class="fa-regular fa-file-lines mr-2" /> Show order history
						</v-dropdown-item>
						<v-dropdown-item @click="closePosition($event, props.row.id)">
							<i class="fa-solid fa-xmark mr-2" /> Close
						</v-dropdown-item>
					</v-dropdown>
				</td>
			</tr>
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
				quantity: "Size",
				entryPrice: "Price",
				breakEvenPrice: "Break Even",
				realizedPnl: "PnL",
				// botLabel: "Bot",
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
<style scoped>
.ws-nowrap {
	white-space: nowrap;
}
</style>
