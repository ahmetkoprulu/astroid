<template>
	<div>
		<page-header title="" :actions="actions" />
		<!-- card shadow-sm -->
		<div class="page-body card-body p-4">
			<v-table
				ref="table"
				:columns="columns"
				:requestFunction="requestFunction"
				:refreshButton="false"
			>
				<template #column-label="props">
					<router-link :to="{ name: 'bot-save', params: { id: props.row.id } }">
						{{ props.row.label }}
					</router-link>
				</template>
				<template #column-isEnabled="props">
					<b-badge pill variant="success" v-if="props.row.isEnabled">
						Active
					</b-badge>
					<b-badge pill variant="default" v-else>Inactive</b-badge>
				</template>
				<template #column-market="props">
					<img
						:src="$consts.EXCHANGE_ICONS[props.row.exchange.providerName]"
						height="20"
					/>
					{{ props.row.exchange.name }}
				</template>
				<template #column-createdDate="props">
					<v-datetime v-model="props.row.createdDate" pretty />
				</template>
				<template #column-actions="props">
					<v-dropdown class="pull-right" variant="link">
						<v-dropdown-item
							@click="showEnableAlert(props.row.id, props.row.isEnabled)"
						>
							<i class="fa-fw fa-solid fa-power-off mr-2" />
							{{ props.row.isEnabled ? "Disable Bot" : "Enable Bot" }}
						</v-dropdown-item>
						<v-dropdown-item @click="showChangeMarginTypeModal(props.row.id)">
							<i class="fa-fw fa-solid fa-shuffle mr-2" /> Change Margin Type
						</v-dropdown-item>
						<v-dropdown-item
							@click="
								() =>
									$router.push({
										name: 'bot-audits',
										params: { id: props.row.id },
									})
							"
						>
							<i class="fa-fw fa-regular fa-file-lines mr-2" /> Audits
						</v-dropdown-item>
						<v-dropdown-divider />
						<v-dropdown-item @click="deleteBot(props.row.id)">
							<span class="text-danger">
								<i class="mr-2 fa-fw fa-solid fa-trash" /> Delete
							</span>
						</v-dropdown-item>
					</v-dropdown>
				</template>
			</v-table>
		</div>
		<ChangeMarginTypeModal ref="changeMarginTypeModal" />
	</div>
</template>

<script>
import Service from "../../services/bots";
import ChangeMarginTypeModal from "../../components/Bots/ChangeMarginTypeModal";

export default {
	data() {
		return {
			actions: [
				{
					title: "Create Bot",
					url: this.$router.resolve({ name: "bot-save" }).href,
					icon: "fa-regular fa-plus",
					variant: "primary",
				},
			],
			columns: {
				label: "Label",
				market: "Market",
				isEnabled: "Enabled",
				createdDate: "Created Date",
				actions: " ",
			},
		};
	},
	methods: {
		async requestFunction(filters, sorts, currentPage, perPage) {
			return await Service.list(filters, sorts, currentPage, perPage);
		},
		async enableBot(id) {
			try {
				const response = await Service.enable(id);
				if (!response.data.success) {
					this.$errorToast(response.data.title, response.data.message);
					return;
				}

				this.$successToast(response.data.title, response.data.message);
				this.$refs.table.refresh();
			} catch (error) {
				this.$errorToast("", error.message);
			}
		},
		async showEnableAlert(id, enabled) {
			if (!enabled) {
				await this.enableBot(id, enabled);
				return;
			}

			this.$alert.remove(
				`Disable the Bot?`,
				"Orders will not be executed",
				async () => {
					await this.enableBot(id, enabled);
				}
			);
		},
		deleteBot(id) {
			this.$alert.remove(
				"Delete the Bot?",
				"You won't be able to undo it",
				async () => {
					try {
						const response = await Service.delete(id);
						if (!response.data.success) {
							this.$errorToast("Delete Bot", response.data.message);
							return;
						}

						this.$successToast("Delete Bot", "Bot deleted successfully");
						this.$refs.table.refresh();
					} catch (error) {
						this.$errorToast("Delete Bot", error.message);
					}
				}
			);
		},
		async showChangeMarginTypeModal(id) {
			this.$refs.changeMarginTypeModal.show(id);
		},
	},
	components: {
		ChangeMarginTypeModal,
	},
};
</script>

<style></style>
