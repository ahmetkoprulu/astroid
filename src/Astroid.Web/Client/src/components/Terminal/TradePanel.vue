<template>
	<b-overlay class="row" :show="busy" no-fade :opacity="1" variant="dark">
		<div class="col-12">
			<b-form-group label="Bot">
				<v-select
					v-model="model.botId"
					:options="botOptions"
					placeholder="Select a bot"
					@input="onBotChange"
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
			</b-form-group>
		</div>
		<div class="col-12">
			<b-form-group label="Symbol">
				<v-select
					v-model="model.ticker"
					:options="symbolOptions"
					placeholder="Select a symbol"
					:disabled="!selectedBot"
					@input="onSymbolChange"
				>
					<div slot="value-label" slot-scope="{ node }">
						<img
							:src="iconUrl + node.label.toLowerCase()"
							class="mr-2"
							height="20"
						/>
						<span>{{ node.label }}<small class="text-muted">/USDT</small></span>
					</div>
					<label slot="option-label" slot-scope="{ node }">
						<img
							:src="iconUrl + node.label.toLowerCase()"
							class="mr-2"
							height="20"
						/>
						<span>{{ node.label }}<small class="text-muted">/USDT</small></span>
					</label>
				</v-select>
			</b-form-group>
		</div>
		<div class="col-12" v-if="isFutures">
			<b-form-group label="Side">
				<v-radio-group
					v-model="model.positionType"
					:options="positionTypeOptions"
					size="sm"
					justify="between"
					width="100%"
					:gap="8"
					fill
					:disabled="!model.botId || !model.ticker || requesting"
				/>
			</b-form-group>
		</div>
		<div class="col-12" v-if="isFutures">
			<b-form-group label="Leverage">
				<b-form-spinbutton
					placeholder="Leverage"
					v-model="model.leverage"
					:disabled="!model.botId || !model.ticker || requesting"
				>
					<template slot="increment">
						<button
							tabindex="-1"
							type="button"
							aria-label="Increment"
							aria-keyshortcuts="ArrowUp"
							class="btn btn-sm border-0 on-background-text rounded-0 py-0"
							style="width: 50"
						>
							<i class="fa-solid fa-plus fa-fw"></i>
						</button>
					</template>
					<template slot="decrement">
						<button
							tabindex="-1"
							type="button"
							aria-label="Increment"
							aria-keyshortcuts="ArrowUp"
							class="btn btn-sm border-0 on-background-text rounded-0 py-0"
							style="width: 50"
						>
							<i class="fa-solid fa-minus fa-fw"></i>
						</button>
					</template>
				</b-form-spinbutton>
			</b-form-group>
		</div>
		<div class="col-12">
			<b-form-group label=" Size">
				<b-input-group
					:class="{ disabled: !model.botId || !model.ticker || requesting }"
				>
					<b-form-input
						type="number"
						class="col-md-12"
						v-model="model.quantity"
					/>
					<b-input-group-append>
						<b-dropdown variant="primary" size="sm" boundary="window">
							<template #button-content>
								<i :class="`mr-1 ${quantityTypeIcons[model.quantityType]}`" />
							</template>
							<b-dropdown-item
								:active="isDropdownItemActive(key)"
								v-for="[key, value] in Object.entries(
									$consts.POSITION_SIZE_TYPES
								)"
								:key="key"
								@click="model.quantityType = Number.parseInt(key)"
							>
								<i :class="`mr-1 ${quantityTypeIcons[key]}`" />
								{{ value }}
							</b-dropdown-item>
						</b-dropdown>
					</b-input-group-append>
				</b-input-group>
			</b-form-group>
		</div>
		<div class="col-6 mt-3 pr-1">
			<button
				class="btn btn-long w-100 mt-2"
				:disabled="!model.botId || !model.ticker || requesting"
				@click="requestPosition('open')"
			>
				Buy
			</button>
		</div>
		<div class="col-6 mt-3 pl-1">
			<button
				class="btn btn-short w-100 mt-2"
				:disabled="!model.botId || !model.ticker || requesting"
				@click="requestPosition('close')"
			>
				Sell
			</button>
		</div>
	</b-overlay>
</template>
<script>
import BotService from "@/services/bots";
import HomeService from "@/services/home";

export default {
	data() {
		return {
			exchangeLabel: {
				"binance-usd-futures": "BINANCE",
				"binance-spot": "BINANCE",
			},
			busy: false,
			requesting: false,
			tickers: [],
			bots: [],
			selectedBot: null,
			iconUrl: "https://coinicons-api.vercel.app/api/icon/",
			model: {
				ticker: null,
				leverage: 1,
				quantity: 50,
				quantityType: 2,
				botId: null,
				positionType: "long",
				key: null,
			},
			quantityTypeIcons: {
				1: "fa-solid fa-percent fa-fw",
				2: "fa-solid fa-dollar-sign fa-fw",
				3: "fa-solid fa-coins fa-fw",
			},
			quantityTypeLabels: {
				1: "percentage",
				2: "fixed-usd",
				3: "fixed-asset",
			},
		};
	},
	computed: {
		botOptions() {
			return this.bots.map((x) => {
				return {
					id: x.id,
					label: `${x.label}`,
					provider: x.exchange.providerName,
				};
			});
		},
		symbolOptions() {
			return this.tickers.map((x) => {
				return {
					id: x,
					label: x,
				};
			});
		},
		positionTypeOptions() {
			return [
				{
					text: "Long",
					value: "long",
				},
				{
					text: "Short",
					value: "short",
				},
			];
		},
		isFutures() {
			return this.selectedBot && this.selectedBot.exchange.providerType == 2;
		},
	},
	async mounted() {
		this.busy = true;
		await this.getBots();
		this.busy = false;
	},
	methods: {
		async requestPosition(type) {
			if (
				!this.selectedBot ||
				!this.selectedBot.key ||
				!this.model.ticker ||
				this.model.leverage < 1 ||
				this.model.quantity < 0 ||
				!this.model.quantityType
			) {
				this.$errorToast("Execute Bot", "Invalid form data");
				return;
			}

			let m = {
				ticker: `${this.model.ticker.toUpperCase()}USDT`,
				leverage: this.model.leverage,
				quantity: this.model.quantity,
				quantityType: this.quantityTypeLabels[this.model.quantityType],
				type: `${type}-${this.model.positionType}`,
				key: this.selectedBot.key,
			};

			this.requesting = true;

			try {
				this.requesting = true;
				const response = await BotService.execute(m);
				if (!response.data.success) {
					this.$errorToast("Request Position", response.data.message);
					this.requesting = false;

					return;
				}

				this.$successToast("Request Position", response.data.message);
			} catch (error) {
				this.$errorToast("Request Position", error.message);
			}
			this.requesting = false;
		},
		async onBotChange(val) {
			var bot = this.bots.find((x) => x.id == val);
			if (
				!this.selectedBot ||
				this.selectedBot.exchange.providerName != bot.exchange.providerName
			) {
				this.selectedBot = bot;
				if (this.selectedBot.exchange.providerType == 2) {
					this.model.leverage = 1;
					this.model.positionType = "long";
				}

				await this.getSymbols();

				return;
			}

			this.selectedBot = bot;
			if (this.selectedBot.exchange.providerType == 2) {
				this.model.leverage = 1;
				this.model.positionType = "long";
			}
		},
		onSymbolChange(symbol) {
			let exchange = this.exchangeLabel[this.selectedBot.exchange.providerName];
			let s = symbol
				? `${exchange}:${symbol}USDT${this.isFutures ? ".P" : ""}`
				: null;
			this.$emit("ticker-changed", s);
		},
		async getBots() {
			try {
				const response = await BotService.getAll();
				if (!response.data.success) {
					this.$errorToast("Fetch Bots", response.data.message);
					return;
				}

				this.bots = response.data.data;
			} catch (error) {
				this.$errorToast("Fetch Bots", error.message);
			}
		},
		async getSymbols() {
			try {
				if (!this.selectedBot) return;

				this.tickers = [];
				this.model.ticker = null;
				const response = await HomeService.getSymbolsByExchangeName(
					this.selectedBot.exchange.providerName
				);
				if (!response.data.success) {
					this.$errorToast("Fetch Bot", response.data.message);
					return;
				}

				this.tickers = response.data.data;
			} catch (error) {
				this.$errorToast("Fetch Bot", error.message);
			}
		},
		isDropdownItemActive(value) {
			return value == this.model.quantityType;
		},
	},
};
</script>
<style>
.btn-long {
	background-color: var(--long) !important;
	background: var(--long) !important;
	border-color: var(--long) !important;
	color: #fff !important;
}

.btn-short {
	background-color: var(--short) !important;
	background: var(--short) !important;
	border-color: var(--short) !important;
	color: #fff !important;
}
</style>
