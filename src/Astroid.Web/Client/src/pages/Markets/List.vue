<template>
	<div>
		<page-header title="Wallets" :actions="actions" />
		<!-- card shadow-sm -->
		<div class="card-body p-4">
			<v-table
				:columns="columns"
				:requestFunction="requestFunction"
				:refreshButton="false"
				ref="table"
			>
				<template #column-name="props">
					<router-link
						:to="{ name: 'market-save', params: { id: props.row.id } }"
					>
						{{ props.row.name }}
					</router-link>
				</template>
				<template #column-providerName="props">
					<img
						:src="$consts.EXCHANGE_ICONS[props.row.providerName]"
						height="20"
					/>
					{{ props.row.providerLabel }}
				</template>
				<template #column-createdDate="props">
					<v-datetime v-model="props.row.createdDate" pretty />
				</template>
				<template #column-actions="props">
					<v-dropdown class="pull-right" variant="link">
						<v-dropdown-item
							@click="
								() =>
									$router.push({
										name: 'market-save',
										params: { id: props.row.id },
									})
							"
						>
							<i class="fa-solid fa-pen-to-square"></i> Edit
						</v-dropdown-item>
						<v-dropdown-item @click="ShowMarginTypeModal(props.row.id)">
							<i class="fa-regular fa-file-lines" /> Audits
						</v-dropdown-item>
						<v-dropdown-divider />
						<v-dropdown-item @click="deleteExchange(props.row.id)">
							<span class="text-danger">
								<i class="mr-1 fa-solid fa-trash" /> Delete
							</span>
						</v-dropdown-item>
					</v-dropdown>
				</template>
			</v-table>
		</div>
	</div>
</template>

<script>
import Service from "../../services/markets";
export default {
	data() {
		return {
			actions: [
				{
					title: "Create Wallet",
					url: this.$router.resolve({ name: "market-save" }).href,
					icon: "fa-regular fa-plus",
					variant: "primary",
				},
			],
			columns: {
				name: "Label",
				providerName: "Market",
				createdDate: "Created Date",
				actions: " ",
			},
		};
	},
	methods: {
		async requestFunction(filters, sorts, currentPage, perPage) {
			return await Service.list(filters, sorts, currentPage, perPage);
		},
		ShowMarginTypeModal(id) {
			this.$router.push({
				name: "market-audits",
				params: { id: id },
			});
		},
		deleteExchange(id) {
			this.$alert.remove(
				"Delete the Market?",
				"You won't be able to undo it",
				async () => {
					try {
						const response = await Service.delete(id);
						if (!response.data.success) {
							this.$errorToast("Delete Market", response.data.message);
							return;
						}

						this.$successToast("Delete Market", "Market deleted successfully");
						this.$refs.table.refresh();
					} catch (error) {
						this.$errorToast("Delete Market", error.message);
					}
				}
			);
		},
	},
};
</script>

<style></style>
