<template>
	<div>
		<page-header title="Save Wallet" :actions="actions" />
		<div class="card-body p-4 col-lg-5 col-md-12">
			<p class="h4 mb-4">General</p>
			<ValidationObserver ref="form">
				<v-validated-input label="Label">
					<b-form-input type="text" v-model="model.name" />
				</v-validated-input>
				<b-form-group label="Description">
					<b-form-textarea v-model="model.description" rows="2" max-rows="6" />
				</b-form-group>
				<v-validated-input label="Market" :rules="!id ? 'required' : ''">
					<v-select
						v-model="model.providerId"
						:options="providerOptions"
						placeholder="Select a market"
						@input="onSelect"
						v-if="!id"
					>
						<div slot="value-label" slot-scope="{ node }">
							<img
								:src="$consts.EXCHANGE_ICONS[node.raw.provider]"
								class="mr-2"
								height="20"
							/>
							<span>{{ node.label }}</span>
						</div>
						<label slot="option-label" slot-scope="{ node }">
							<img
								:src="$consts.EXCHANGE_ICONS[node.raw.provider]"
								class="mr-2"
								height="20"
							/>
							<span>{{ node.label }}</span>
						</label>
					</v-select>
					<span v-else>
						<span>
							<img
								:src="$consts.EXCHANGE_ICONS[model.providerName]"
								class="mr-2"
								height="20"
							/>
						</span>
						<span class="mt-2">{{ model.providerLabel }}</span>
					</span>
				</v-validated-input>
				<v-validated-input
					:label="property.displayName"
					:description="property.description"
					v-for="property in this.model.properties"
					:key="property.property"
					:rules="property.required ? 'required' : ''"
				>
					<v-dynamic-input v-model="property.value" :property="property" />
				</v-validated-input>
			</ValidationObserver>
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
					title: "Delete",
					event: () => this.delete(),
					icon: "fas fa-trash",
					variant: "light",
					hidden: () => !this.id,
				},
				{
					title: "Save",
					event: (b) => this.save(b),
					icon: "fas fa-plus",
					variant: "primary",
				},
			],
			model: {
				name: null,
				description: null,
				providerId: null,
				properties: [],
			},
			id: null,
			providers: [],
		};
	},
	computed: {
		providerOptions() {
			return this.providers.map((provider) => {
				return {
					id: provider.id,
					label: provider.title,
					provider: provider.name,
				};
			});
		},
	},
	async mounted() {
		this.$busy = true;
		this.id = this.$route.params.id;
		try {
			if (this.id) {
				const response = await Service.get(this.id);
				if (!response.data.success) {
					this.$errorToast("Fetch Market", response.data.message);
					return;
				}

				this.model = response.data.data;
			} else {
				await this.getMarketProviders();
			}
		} catch (error) {
			this.$errorToast("Fetch Market", error.message);
		}
		this.$busy = false;
	},
	methods: {
		async getMarketProviders() {
			const response = await Service.getProviders();
			this.providers = response.data.data;
		},
		async save(b) {
			const isValid = await this.$refs.form.validate();
			if (!isValid) return;

			b.setBusy(true);
			try {
				var response = await Service.save(this.model);
				if (!response.data.success) {
					this.$errorToast("Save Wallet", response.data.message);
					return;
				}

				this.$successToast("Save Wallet", "Market saved successfully");
				this.$router.push({ name: "market-list" });
			} catch (error) {
				console.error(error);
			} finally {
				b.setBusy(false);
			}
		},
		delete() {
			this.$alert.remove(
				"Delete the Market?",
				"You won't be able to undo it",
				async () => {
					try {
						var response = await Service.delete(this.id);
						if (!response.data.success) {
							this.$errorToast("Delete Market", response.data.message);
							return;
						}

						this.$successToast("Delete Market", "Market deleted successfully");
						this.$router.push({ name: "market-list" });
					} catch (error) {
						this.$errorToast("Delete Market", error.message);
					}
				}
			);
		},
		onSelect(id) {
			const provider = this.providers.find((x) => x.id === id);
			this.model.properties = Object.assign(
				this.model.properties,
				provider.properties
			);
		},
	},
};
</script>

<style></style>
