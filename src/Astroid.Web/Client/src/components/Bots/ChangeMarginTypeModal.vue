<template>
  <b-modal
    ref="modal"
    title="Change Margin Type"
    :ok-disabled="isBusy"
    @ok.prevent="ok"
    @hidden="hidden"
    ok-only
  >
    <b-form-group
      label="Ticker(s)"
      description="Tickers in comma seperated format"
    >
      <b-form-input v-model="tickers" placeholder="BTCUSDT,ETHUSDT..." />
    </b-form-group>
    <b-form-group label="Margin Type">
      <v-select
        v-model="marginType"
        :options="options"
        placeholder="Select a margin type"
      />
    </b-form-group>
  </b-modal>
</template>

<script>
import Service from "../../services/bots";
export default {
  data() {
    return {
      botId: null,
      marginType: null,
      tickers: null,
      isBusy: false,
      options: [
        { label: "Isolated", id: 0 },
        { label: "Cross", id: 1 },
      ],
    };
  },
  methods: {
    show(id) {
      this.botId = id;
      this.$refs.modal.show();
    },
    async ok() {
      this.isBusy = true;
      try {
        var response = await Service.changeMarginType(
          this.botId,
          this.marginType,
          this.tickers
        );
        if (!response.data.success) return;
        this.$refs.modal.hide();
      } catch (err) {
        console.error(err);
      } finally {
        this.isBusy = false;
      }
    },
    hidden() {
      this.marginType = null;
      this.tickers = null;
      this.botId = null;
      this.isBusy = false;
    },
  },
};
</script>

<style></style>
