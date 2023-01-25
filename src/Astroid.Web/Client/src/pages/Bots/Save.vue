<template>
  <div>
    <page-header title="Save Bot" :actions="actions" />
    <div class="row">
      <div class="col-lg-6 col-md-12">
        <b-form-group label="Label">
          <b-form-input type="text" v-model="model.label" />
        </b-form-group>
        <b-form-group label="Description">
          <b-form-textarea v-model="model.description" rows="2" max-rows="6" />
        </b-form-group>
        <b-form-group label="Market">
          <v-select
            v-model="model.exchangeId"
            :options="exchangeOptions"
            placeholder="Select a market"
          />
        </b-form-group>
        <b-form-group
          label="Position Size"
          description="In ratio of avaible balance"
        >
          <b-form-input type="number" v-model="model.positionSize" />
        </b-form-group>
        <b-form-group label="Take Profit">
          <b-form-checkbox v-model="model.isTakePofitEnabled" switch />
        </b-form-group>
        <b-form-group
          label="Profit Target"
          description="In ratio of entry price"
          v-if="model.isTakePofitEnabled"
        >
          <b-form-input type="number" v-model="model.profitActivation" />
        </b-form-group>
        <b-form-group label="Stop Loss">
          <b-form-checkbox v-model="model.isStopLossEnabled" switch />
        </b-form-group>
        <b-form-group
          label="Loss Activation"
          description="In ratio of entry price"
          v-if="model.isStopLossEnabled"
        >
          <b-form-input type="number" v-model="model.stopLossActivation" />
        </b-form-group>
        <b-form-group label="Enabled">
          <b-form-checkbox v-model="model.isEnabled" switch />
        </b-form-group>
      </div>
      <div class="col-lg-6 col-md-12">
        <b-form-group label="Open Long">
          <div>
            {{
              `https://trade.ahmetkoprulu.com/api/strategies/${model.key}/open-long`
            }}
          </div>
        </b-form-group>
        <b-form-group label="Close Long">
          <div>
            {{
              `https://trade.ahmetkoprulu.com/strategies/${model.key}/close-long`
            }}
          </div>
        </b-form-group>
        <b-form-group label="Open Short">
          <div>
            {{
              `https://trade.ahmetkoprulu.com/strategies/${model.key}/open-short`
            }}
          </div>
        </b-form-group>
        <b-form-group label="Close Short">
          <div>
            {{
              `https://trade.ahmetkoprulu.com/strategies/${model.key}/close-short`
            }}
          </div>
        </b-form-group>
        <b-form-group label="Message">
          <textarea v-model="bodyExample" readonly rows="4"></textarea>
        </b-form-group>
      </div>
    </div>
  </div>
</template>

<script>
import Service from "@/services/bots";
import MarketService from "@/services/markets";

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
          event: () => this.save(),
          icon: "fas fa-plus",
          variant: "primary",
        },
      ],
      model: {
        label: null,
        description: null,
        exchangeId: null,
        positionSize: 10,
        isTakePofitEnabled: false,
        profitActivation: 1.3,
        isStopLossActivated: false,
        stopLossActivation: 0.9,
        key: "",
        isEnabled: false,
      },
      id: null,
      markets: [],
      bodyExample: JSON.stringify(
        { ticker: "BTCUSDT", leverage: 20, orderType: "open-short" },
        null,
        4
      ),
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
  },
  async mounted() {
    this.id = this.$route.params.id;
    await this.getMarketProviders();

    if (this.id) {
      const response = await Service.get(this.id);
      this.model = response.data.data;
    } else {
      this.model.key = window.crypto.randomUUID();
    }
  },
  methods: {
    async getMarketProviders() {
      this.$busy = true;
      const response = await MarketService.getAll();
      this.markets = response.data.data;
      this.$busy = false;
    },
    async save() {
      try {
        const response = await Service.save(this.model);
        if (response.status !== 200) {
          return;
        }
        this.$router.push({ name: "bot-list" });
      } catch (error) {
        console.error(error);
      }
    },
    delete() {
      this.$alert.remove(
        "Delete the Bot?",
        "You won't be able to undo it",
        async () => {
          try {
            await Service.delete(this.id);
            this.$router.push({ name: "bot-list" });
          } catch (error) {
            console.error(error);
          }
        }
      );
    },
  },
};
</script>

<style></style>
