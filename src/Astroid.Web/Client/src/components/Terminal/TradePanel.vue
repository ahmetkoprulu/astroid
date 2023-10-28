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
					@input="onSymbolChange"
				>
					<div slot="value-label" slot-scope="{ node }">
						<img
							:src="iconUrl + node.label.toLowerCase()"
							class="mr-2"
							height="20"
						/>
						<span>{{ node.label }}</span>
					</div>
					<label slot="option-label" slot-scope="{ node }">
						<img
							:src="iconUrl + node.label.toLowerCase()"
							class="mr-2"
							height="20"
						/>
						<span>{{ node.label }}</span>
					</label>
				</v-select>
			</b-form-group>
		</div>
		<div class="col-12">
			<b-form-group label="Leverage">
				<b-form-spinbutton placeholder="Leverage" v-model="model.leverage">
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
			<b-form-group label="Position Size">
				<b-input-group>
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
		<div class="col-6 mt-4 pr-1">
			<b-button
				variant="success"
				class="w-100 mt-2"
				:disabled="!model.botId || !model.ticker || requesting"
				@click="requestPosition('open-long')"
			>
				Long
			</b-button>
		</div>
		<div class="col-6 mt-4 pl-1">
			<b-button
				variant="danger"
				class="w-100 mt-2"
				:disabled="!model.botId || !model.ticker || requesting"
				@click="requestPosition('open-short')"
			>
				Short
			</b-button>
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
			},
			busy: false,
			requesting: false,
			tickers: [],
			bots: [],
			selectedBot: null,
			iconUrl: "https://coinicons-api.vercel.app/api/icon/",
			model: {
				ticker: null,
				leverage: 5,
				quantity: 50,
				quantityType: 2,
				botId: null,
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
	},
	async mounted() {
		this.busy = true;
		await this.getBots();
		this.busy = false;
	},
	methods: {
		async requestPosition(side) {
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
				type: side,
				key: this.selectedBot.key,
			};

			this.requesting = true;

			try {
				this.requesting = true;
				const response = await BotService.execute(m);
				if (!response.data.success) {
					this.$errorToast("Execute Bot", response.data.message);
					this.requesting = false;

					return;
				}

				this.$successToast("Execute Bot", response.data.message);
			} catch (error) {
				this.$errorToast("Execute Bot", error.message);
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
				await this.getSymbols();

				return;
			}

			this.selectedBot = bot;
		},
		onSymbolChange(symbol) {
			let exchange = this.exchangeLabel[this.selectedBot.exchange.providerName];
			let s = symbol ? `${exchange}:${symbol}USDT.P` : null;
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
