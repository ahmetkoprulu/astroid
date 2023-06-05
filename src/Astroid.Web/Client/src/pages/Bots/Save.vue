<template>
	<div>
		<page-header title="Save Bot" :actions="actions" />
		<div class="row mx-md-3">
			<div class="col-lg-5 col-md-12">
				<ValidationObserver ref="form">
					<div class="d-flex justify-content-between">
						<v-validated-input label="Label" class="w-100 mr-3">
							<b-form-input type="text" v-model="model.label" />
						</v-validated-input>
						<b-form-group label="Enabled">
							<b-form-checkbox
								v-model="model.isEnabled"
								switch
								style="margin-top: 7px"
							/>
						</b-form-group>
					</div>
					<b-form-group label="Description">
						<b-form-textarea
							v-model="model.description"
							rows="2"
							max-rows="6"
						/>
					</b-form-group>
					<v-validated-input label="Market">
						<v-select
							v-model="model.exchangeId"
							:options="exchangeOptions"
							placeholder="Select a market"
						/>
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
							width="120px"
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
								name="computation-methods"
								v-model="model.limitSettings.computationMethod"
								:options="limitOrderBookComputationMethodOptions"
								split
								@click="showEditorModal"
							/>
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
					/>
				</b-form-group>
				<b-form-group label="Take Profit">
					<b-form-checkbox v-model="model.isTakePofitEnabled" switch />
				</b-form-group>
				<b-form-group v-if="model.isTakePofitEnabled">
					<template #label>
						<div class="d-flex justify-content-between">
							<span>Profit Targets</span>
							<a
								href="javascript:;"
								@click="
									model.takeProfitTargets.push({
										activation: null,
										share: null,
									})
								"
							>
								<i class="fas fa-plus fa-fw" />
							</a>
						</div>
					</template>
					<MultipleTakeProfit v-model="model.takeProfitTargets" />
				</b-form-group>
				<b-form-group label="Stop Loss">
					<b-form-checkbox v-model="model.isStopLossEnabled" switch />
				</b-form-group>
				<b-form-group
					label="Stop Price"
					description="In ratio of entry price"
					v-if="model.isStopLossEnabled"
				>
					<b-form-input type="number" v-model="model.stopLossPrice" />
				</b-form-group>
				<div class="d-flex">
					<b-form-group
						class="w-50 mr-2"
						label="Callback Rate"
						description="Percentage of price change to trigger"
						v-if="model.isStopLossEnabled"
					>
						<b-form-input type="number" v-model="model.stopLossCallbackRate" />
					</b-form-group>
					<b-form-group
						class="w-50"
						label="Activation Price"
						v-if="model.isStopLossEnabled"
					>
						<b-form-input type="number" v-model="model.stopLossActivation" />
					</b-form-group>
				</div>
			</div>
			<div class="col-lg-7 col-md-12">
				<WebhookInfo :bot-key="model.key" />
			</div>
		</div>
		<b-modal ref="editorModal" title="Code Editor" size="xl">
			<code class="code code-top">
				<span class="text-primary"> public <i>decimal</i> </span>
				<strong> ComputeEntryPoint </strong> (
				<i class="text-secondary">List&lt;OrderBookEntry&gt; entries </i>)
				<strong>{</strong>
			</code>
			<CodeEditor v-model="model.limitSettings.code" />
			<code class="code code-bottom">}</code>
		</b-modal>
	</div>
</template>

<script>
import Service from "@/services/bots";
import MarketService from "@/services/markets";

import MultipleTakeProfit from "@/components/Bots/MultipleTakeProfit.vue";
import WebhookInfo from "@/components/Bots/WebhookInfo.vue";
import CodeEditor from "@/components/shared/code-editor/CodeEditor.vue";
import DropDownSelect from "@/components/shared/DropdownSelect.vue";

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
				positionSizeType: 1,
				limitSettings: {
					valorizationType: 2,
					forceUntilFilled: false,
					computeEntryPoint: false,
					computationMethod: 1,
					code: "// Entries are bids for long and asks for short.\n// Order book depth is 1000.\n// Order book is sorted by price.\n\n",
					orderBookSkip: 1,
					orderBookOffset: 3,
					deviation: 1,
				},
				isTakePofitEnabled: false,
				takeProfitTargets: [],
				isStopLossActivated: false,
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
					label: `${x.name} (${x.providerName})`,
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
			if (this.model.limitSettings.computationMethod != 2) return;
			this.$refs.editorModal.show();
		},
	},
	components: {
		WebhookInfo,
		MultipleTakeProfit,
		CodeEditor,
		DropDownSelect,
	},
};
</script>
<style scoped>
.code {
	width: 100%;
	font-size: 11pt;
	display: block;
	border-radius: 0px;
	background-color: #eee;
	border-left: 1px solid #ddd;
	border-right: 1px solid #ddd;
}
.code-top {
	border-top: 1px solid #ddd;
}
.code-bottom {
	border-bottom: 1px solid #ddd;
}
</style>
