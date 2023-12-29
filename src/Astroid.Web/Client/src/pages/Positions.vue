<template>
	<div>
		<page-header title="" />
		<!-- card shadow-sm -->
		<div class="page-body card-body p-4">
			<v-table
				:columns="columns"
				:requestFunction="requestFunction"
				:refreshButton="false"
				ref="table"
			>
				<template #row-base="props">
					<tr block v-b-toggle="props.row.id">
						<td>
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
						</td>
						<td>
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
						</td>
						<td>
							<div>
								<span class="text-muted">entry: </span
								>{{ props.row.entryPrice }}
							</div>
							<div>
								<span class="text-muted">avg: </span
								>{{ props.row.averagePrice }}
							</div>
						</td>
						<td>{{ props.row.leverage }}</td>
						<td>{{ props.row.realizedPnl }} USDT</td>
						<td>{{ props.row.botLabel }}</td>
						<td>
							<img :src="icons[props.row.exchangeProviderName]" height="20" />
							{{ props.row.exchangeLabel }}
						</td>
						<td>
							<b-badge
								pill
								:variant="
									props.row.status == 4 || props.row.status == 5
										? 'danger'
										: 'light'
								"
								>{{ $consts.POSITION_STATUS[props.row.status].title }}</b-badge
							>
						</td>
						<td><v-datetime v-model="props.row.createdDate" pretty /></td>
						<td>
							<v-dropdown class="pull-right" variant="link">
								<v-dropdown-item @click="showHistory($event, props.row.orders)">
									<i class="fa-regular fa-file-lines mr-2" /> Show order history
								</v-dropdown-item>
								<v-dropdown-item @click="closePosition($event, props.row.id)">
									<i class="fa-solid fa-xmark mr-2" /> Close
								</v-dropdown-item>
							</v-dropdown>
						</td>
					</tr>
					<tr v-if="props.row.status == 1">
						<td class="p-0" colspan="9">
							<b-collapse
								class="w-100"
								style="height: 50px"
								:id="props.row.id"
								:accordion="props.row.id"
								role="tabpanel"
							>
								<span v-if="props.row.orders.length == 0">No open orders</span>
								<OrderHistoryTable
									v-else
									:orders="
										props.row.orders.filter(
											(x) => x.status != 4 && x.status != 5
										)
									"
								/>
							</b-collapse>
						</td>
					</tr>
				</template>
			</v-table>
		</div>
		<OrderHistoryModal ref="orderHistoryModal" />
	</div>
</template>

<script>
import Service from "../services/positions";
import { EXCHANGE_ICONS } from "../core/consts";

import OrderHistoryModal from "../components/Positions/PositionHistory.vue";
import OrderHistoryTable from "../components/Positions/OrdersTable.vue";

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
				exchangeLabel: "Market",
				status: "Status",
				createdDate: "Created Date",
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
		async requestFunction(filters, sorts, currentPage, perPage) {
			return await Service.list(filters, sorts, currentPage, perPage);
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
	},
	components: { OrderHistoryModal, OrderHistoryTable },
};
</script>

<style></style>
