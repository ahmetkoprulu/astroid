<template>
	<div>
		<page-header title="Positions" />
		<!-- card shadow-sm -->
		<div class="">
			<v-table
				:columns="columns"
				:requestFunction="requestFunction"
				:refreshButton="false"
				ref="table"
			>
				<template #row-base="props">
					<tr
						:class="{ 'bg-light': props.row.status == 2 }"
						block
						v-b-toggle="props.row.id"
					>
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
						<td>{{ props.row.quantity }}</td>
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
						<td>{{ props.row.botLabel }}</td>
						<td>
							<img :src="icons[props.row.exchangeProviderName]" height="20" />
							{{ props.row.exchangeLabel }}
						</td>
						<td><v-datetime v-model="props.row.createdDate" pretty /></td>
						<td>
							<v-dropdown class="pull-right">
								<v-dropdown-item @click="showHistory(props.row.orders)">
									<i class="fa-solid fa-xmark" /> Show Order History
								</v-dropdown-item>
								<v-dropdown-item @click="closePosition(props.row.id)">
									<i class="fa-solid fa-xmark" /> Close
								</v-dropdown-item>
							</v-dropdown>
						</td>
					</tr>
					<tr v-if="props.row.status !== 2">
						<td class="p-0" colspan="8">
							<b-collapse
								class="w-100"
								style="height: 50px"
								:id="props.row.id"
								:accordion="props.row.id"
								role="tabpanel"
							>
								<span v-if="props.row.orders.length == 0">No open orders</span>
								<table
									class="table table-condensed mt-0 w-100 d-block d-md-table flex-grow-1"
									v-else
								>
									<thead>
										<tr>
											<th>Type</th>
											<th>Quantity</th>
											<th>Trigger Price</th>
											<th>Close Position</th>
											<th>Status</th>
										</tr>
									</thead>
									<tbody>
										<tr
											v-for="order in props.row.orders.filter(
												(x) => x.status != 4 && x.status != 5
											)"
											:key="order.id"
										>
											<td>
												<b-badge pill variant="light">{{
													$consts.ORDER_TRIGGER_TYPES[order.triggerType].title
												}}</b-badge>
											</td>
											<td>{{ order.quantity }}</td>
											<td>{{ order.triggerPrice }}</td>
											<td>
												<b-badge
													pill
													:variant="order.closePosition ? 'success' : 'light'"
													>{{ order.closePosition ? "Yes" : "No" }}
												</b-badge>
											</td>
											<td>
												<b-badge
													pill
													:variant="
														order.status == 4 || order.status == 5
															? 'danger'
															: 'light'
													"
													>{{
														$consts.ORDER_STATUS[order.status].title
													}}</b-badge
												>
											</td>
										</tr>
									</tbody>
								</table>
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
export default {
	data() {
		return {
			columns: {
				symbol: "Symbol",
				quantity: "Quantity",
				entryPrice: "Price",
				leverage: "Leverage",
				botLabel: "Bot",
				exchangeLabel: "Market",
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
		showHistory(orders) {
			this.$refs.orderHistoryModal.show(orders);
		},
		closePosition(id) {
			console.log(id);
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
	components: { OrderHistoryModal },
};
</script>

<style></style>
