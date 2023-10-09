<template>
	<div>
		<page-header title="Save Bot" :actions="actions" />
		<div class="row mx-md-3">
			<div class="col-lg-5 col-md-12">
				<ValidationObserver ref="form">
					<div class="d-flex justify-content-between">
						<v-validated-input label="Label" class="w-100">
							<b-form-input type="text" v-model="model.label" />
						</v-validated-input>
						<!-- <b-form-group label="Enabled">
							<b-form-checkbox
								v-model="model.isEnabled"
								switch
								style="margin-top: 7px"
							/>
						</b-form-group> -->
					</div>
					<b-form-group label="Description">
						<b-form-textarea
							v-model="model.description"
							rows="2"
							max-rows="6"
						/>
					</b-form-group>
					<v-validated-input label="Wallet">
						<v-select
							v-model="model.exchangeId"
							:options="exchangeOptions"
							placeholder="Select a wallet"
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
					</v-validated-input>
				</ValidationObserver>
				<b-form-group label="Position Size">
					<b-input-group>
						<b-form-input
							type="number"
							class="w-75"
							v-model="model.positionSize"
						/>
						<b-input-group-append>
							<b-dropdown variant="primary" size="sm" boundary="window">
								<template #button-content>
									<i
										:class="`mr-1 ${
											positionSizeTypeIcons[model.positionSizeType]
										}`"
									/>
								</template>
								<b-dropdown-item
									:active="isDropdownItemActive(key)"
									v-for="[key, value] in Object.entries(
										$consts.POSITION_SIZE_TYPES
									)"
									:key="key"
									@click="model.positionSizeType = Number.parseInt(key)"
								>
									<i :class="`mr-1 ${positionSizeTypeIcons[key]}`" />
									{{ value }}
								</b-dropdown-item>
							</b-dropdown>
						</b-input-group-append>
					</b-input-group>
				</b-form-group>
				<b-form-group
					label="Expandable Position Size"
					description="Orders will increase position size if position of same side already exists"
				>
					<b-form-checkbox v-model="model.isPositionSizeExpandable" />
				</b-form-group>
				<b-form-group label="Order Type">
					<v-radio-group
						v-model="model.orderType"
						:options="orderEntryTypeOptions"
					/>
				</b-form-group>
				<div v-if="model.orderType === 2">
					<b-form-group label="Valorization Type">
						<v-radio-group
							v-model="model.limitSettings.valorizationType"
							:options="limitDeviationOptions"
							width="140px"
						/>
					</b-form-group>
					<div v-if="model.limitSettings.valorizationType !== 1">
						<b-form-group label="Compute Entry Point">
							<b-form-checkbox
								v-model="model.limitSettings.computeEntryPoint"
								switch
							/>
						</b-form-group>
						<b-form-group
							label="Computation Method"
							v-if="model.limitSettings.computeEntryPoint"
						>
							<DropDownSelect
								class="mr-3"
								name="computation-methods"
								v-model="model.limitSettings.computationMethod"
								:options="limitOrderBookComputationMethodOptions"
								split
								@click="showEditorModal"
							/>
							<a
								href="#"
								class="badge btn label-primary"
								style="padding: 9px"
								@click="showComputationTestModal"
							>
								<i class="fa-solid fa-flask fa-fw" /> Test
							</a>
						</b-form-group>
						<b-form-group
							label="Force Until Position Filled"
							description="Place multiple orders until the position size match"
						>
							<b-form-checkbox
								v-model="model.limitSettings.forceUntilFilled"
								switch
							/>
						</b-form-group>
						<b-form-group
							label="Skip"
							v-if="!model.limitSettings.forceUntilFilled"
						>
							<b-form-input
								type="number"
								v-model="model.limitSettings.orderBookSkip"
							/>
						</b-form-group>
						<b-form-group
							label="Offset"
							v-if="!model.limitSettings.forceUntilFilled"
						>
							<b-form-input
								type="number"
								v-model="model.limitSettings.orderBookOffset"
							/>
						</b-form-group>
					</div>
					<div v-else>
						<b-form-group label="Deviation">
							<b-form-input
								type="number"
								v-model="model.limitSettings.deviation"
							/>
						</b-form-group>
						<b-form-group label="Order Timeout" description="In seconds">
							<b-form-input
								type="number"
								v-model="model.limitSettings.orderTimeout"
							/>
						</b-form-group>
					</div>
				</div>
				<b-form-group label="Order Mode">
					<v-radio-group
						v-model="model.orderMode"
						:options="orderTypeOptions"
						width="130px"
					/>
				</b-form-group>
				<b-form-group label="Pyramiding">
					<b-form-checkbox v-model="model.isPyramidingEnabled" switch />
				</b-form-group>
				<b-form-group label="Entry Target(s)" v-if="model.isPyramidingEnabled">
					<MultipleQuantityTarget
						v-model="model.pyramidingSettings.targets"
						quantityLabel="Quantity (Position Size)"
						targetLabel="Target (%)(+/-)"
					/>
				</b-form-group>
				<b-form-group label="Take Profit">
					<b-form-checkbox v-model="model.isTakePofitEnabled" switch />
				</b-form-group>
				<div v-if="model.isTakePofitEnabled">
					<b-form-group label="Price Reference">
						<v-radio-group
							v-model="model.takeProfitSettings.calculationBase"
							:options="priceReferenceOptions"
						/>
					</b-form-group>
					<b-form-group
						label="Profit Target(s)"
						v-if="model.isTakePofitEnabled"
					>
						<MultipleQuantityTarget
							v-model="model.takeProfitSettings.targets"
							quantityLabel="Quantity (%)"
						/>
					</b-form-group>
				</div>
				<b-form-group label="Stop Loss">
					<b-form-checkbox v-model="model.isStopLossEnabled" switch />
				</b-form-group>
				<div v-if="model.isStopLossEnabled">
					<b-form-group label="Type">
						<v-radio-group
							v-model="model.stopLossType"
							:options="stopLossOptions"
							width="150px"
						/>
					</b-form-group>
					<b-form-group
						label="Trigger Price"
						description="In ratio of entry price"
					>
						<b-form-input type="number" v-model="model.stopLossPrice" />
					</b-form-group>
					<b-form-group
						class="w-50 mr-2"
						label="Callback Rate"
						description="Percentage of price change to trigger"
						v-if="model.stopLossType === 2"
					>
						<b-form-input type="number" v-model="model.stopLossCallbackRate" />
					</b-form-group>
				</div>
			</div>
			<div class="col-lg-7 col-md-12">
				<WebhookInfo :bot-key="model.key" />
			</div>
		</div>
		<ComputationMethodModal
			ref="computationMethodModal"
			:settings="model.limitSettings"
		/>
		<ComputationMethodTestModal ref="computationMethodTestModal" :bot="model" />
	</div>
</template>

<script>
import Service from "@/services/bots";
import MarketService from "@/services/markets";

import MultipleQuantityTarget from "@/components/Bots/MultipleQuantityTarget.vue";
import WebhookInfo from "@/components/Bots/WebhookInfo.vue";
import DropDownSelect from "@/components/shared/DropdownSelect.vue";
import ComputationMethodModal from "@/components/Bots/ComputationMethodModal.vue";
import ComputationMethodTestModal from "@/components/Bots/ComputationMethodTestModal.vue";

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
			positionSizeTypeIcons: {
				1: "fa-solid fa-percent fa-fw",
				2: "fa-solid fa-dollar-sign fa-fw",
				3: "fa-solid fa-coins fa-fw",
			},
			orderModeIcons: {
				1: "fa-solid fa-arrow-up fa-fw",
				2: "fa-solid fa-arrows-up-down fa-fw",
				3: "fa-solid fa-arrow-down-up-across-line fa-fw",
			},
			orderEntryTypeIcons: {
				1: "fa-solid fa-chart-line fa-fw",
				2: "fa-solid fa-list fa-fw",
			},
			valorizationTypeIcons: {
				1: "fa-solid fa-tag fa-fw",
				2: "fa-solid fa-book-open fa-fw",
			},
			model: {
				label: null,
				description: null,
				exchangeId: null,
				orderType: 1,
				positionSize: 10,
				isPositionSizeExpandable: true,
				orderMode: 2,
				positionSizeType: 2,
				limitSettings: {
					valorizationType: 2,
					forceUntilFilled: false,
					computeEntryPoint: false,
					computationMethod: 1,
					orderBookDepth: 1000,
					code: "// Entries are asks for long and bids for short.\n// Entries are sorted by ascending for asks and descending for bids.\n// Entry object consist of Price and Quantity properties.\n// Referenced namespaces:\n// using System;\n// using System.Collections.Generic;\n// using System.Linq;\n\n",
					orderBookSkip: 1,
					orderBookOffset: 3,
					deviation: 1,
				},
				isPyramidingEnabled: false,
				pyramidingSettings: {
					targets: [],
				},
				isTakePofitEnabled: false,
				takeProfitSettings: {
					calculationBase: 1,
					targets: [],
				},
				isStopLossActivated: false,
				stopLossType: 1,
				stopLossActivation: null,
				stopLossCallbackRate: null,
				stopLossPrice: null,
				key: "",
				isEnabled: false,
			},
			id: null,
			markets: [],
			bodyExample: JSON.stringify({ ticker: "BTCUSDT", leverage: 20 }, null, 4),
		};
	},
	computed: {
		baseUrl() {
			window.location.origin;
			return `${window.location.origin}/api/hooks`;
		},
		exchangeOptions() {
			return this.markets.map((x) => {
				return {
					id: x.id,
					label: `${x.name}`,
					provider: x.providerName,
				};
			});
		},
		orderTypeOptions() {
			return Object.entries(this.$consts.ORDER_MODE_TYPES)
				.slice(1)
				.map(([key, value]) => {
					return {
						text: value,
						value: Number.parseInt(key),
						icon: this.orderModeIcons[key],
					};
				});
		},
		orderEntryTypeOptions() {
			return Object.entries(this.$consts.ORDER_ENTRY_TYPES)
				.slice(1)
				.map(([key, value]) => {
					return {
						text: value,
						value: Number.parseInt(key),
						icon: this.orderEntryTypeIcons[key],
					};
				});
		},
		limitDeviationOptions() {
			return Object.entries(this.$consts.LIMIT_VALORIZATION_TYPES).map(
				([key, value]) => {
					return {
						text: value,
						value: Number.parseInt(key),
						icon: this.valorizationTypeIcons[key],
					};
				}
			);
		},
		limitOrderBookComputationMethodOptions() {
			return Object.entries(
				this.$consts.LIMIT_ORDER_BOOK_COMPUTATION_METHODS
			).map(([_, value]) => value); // eslint-disable-line no-unused-vars
		},
		stopLossOptions() {
			return Object.entries(this.$consts.STOP_LOSS_TYPE).map(([key, value]) => {
				return {
					text: value.title,
					value: Number.parseInt(key),
					icon: value.icon,
				};
			});
		},
		priceReferenceOptions() {
			return Object.entries(this.$consts.PRICE_REFERENCE_TYPES).map(
				([key, value]) => {
					return {
						text: value.title,
						value: Number.parseInt(key),
						icon: value.icon,
					};
				}
			);
		},
	},
	async mounted() {
		this.$busy = true;
		this.id = this.$route.params.id;

		try {
			await this.getMarketProviders();

			if (this.id) {
				const response = await Service.get(this.id);
				if (!response.data.success) {
					this.$errorToast("Fetch Bot", response.data.message);
					return;
				}

				this.model = response.data.data;
			} else {
				this.model.key = window.crypto.randomUUID();
			}
		} catch (error) {
			this.$errorToast("Fetch Bot", error.message);
		}

		this.$busy = false;
	},
	methods: {
		async getMarketProviders() {
			const response = await MarketService.getAll();
			if (!response.data.success) {
				this.$errorToast("Fetch Markets", response.data.message);
				return;
			}

			this.markets = response.data.data;
		},
		async save(b) {
			const isValid = await this.$refs.form.validate();
			if (!isValid) return;

			b.setBusy(true);
			try {
				const response = await Service.save(this.model);
				if (!response.data.success) {
					this.$errorToast("Save Bot", response.data.message);
					return;
				}

				this.$successToast("Save Bot", "Bot saved successfully");
				this.$router.push({ name: "bot-list" });
			} catch (error) {
				console.error(error);
			} finally {
				b.setBusy(false);
			}
		},
		delete() {
			this.$alert.remove(
				"Delete the Bot?",
				"You won't be able to undo it",
				async () => {
					try {
						const response = await Service.delete(this.id);
						if (!response.data.success) {
							this.$errorToast("Delete Bot", response.data.message);
							return;
						}

						this.$successToast("Delete Bot", "Bot deleted successfully");
						this.$router.push({ name: "bot-list" });
					} catch (error) {
						this.$errorToast("Delete Bot", error.message);
					}
				}
			);
		},
		isDropdownItemActive(value) {
			return value == this.model.positionSizeType;
		},
		showEditorModal() {
			this.$refs.computationMethodModal.show();
		},
		showComputationTestModal() {
			this.$refs.computationMethodTestModal.show();
		},
	},
	components: {
		WebhookInfo,
		MultipleQuantityTarget,
		DropDownSelect,
		ComputationMethodModal,
		ComputationMethodTestModal,
	},
};
</script>
