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
		<template #column-realizedPnl="props">
			{{ props.row.realizedPnl }} USDT
		</template>
		<template #column-status="props">
			<b-badge pill :variant="props.row.status == 4 ? 'danger' : 'light'">
				{{ $consts.POSITION_STATUS[props.row.status].title }}
			</b-badge>
		</template>
		<template #column-createdDate="props">
			<v-datetime v-model="props.row.createdDate" pretty />
		</template>
	</v-table>
</template>
<script>
import Service from "@/services/positions";
import { EXCHANGE_ICONS } from "@/core/consts";

export default {
	data() {
		return {
			columns: {
				symbol: "Symbol",
				quantity: "Quantity",
				entryPrice: "Price",
				leverage: "Leverage",
				realizedPnl: "Est. RPnL",
				status: "Status",
				createdDate: "Created At",
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
			return await Service.listHistory(filters, sorts, currentPage, 5);
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
};
</script>
