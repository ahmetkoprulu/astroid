<template>
  <div>
    <b-form-group label="Webhook">
      {{ `https://trade.ahmetkoprulu.com/api/bots/${botKey}/execute` }}
    </b-form-group>
    <b-form-group label="Open Long">
      <pre class="code">
		{ "ticker": "BTCUSDT", "leverage": 20, "type": "open-long"}
      </pre>
    </b-form-group>
    <b-form-group label="Close Long">
      <pre class="code">
		{ "ticker": "BTCUSDT", "type": "close-long"}
      </pre>
    </b-form-group>
    <b-form-group label="Open Short">
      <pre class="code">
		{ "ticker": "BTCUSDT", "leverage": 20, "type": "open-short"}
      </pre>
    </b-form-group>
    <b-form-group label="Close Short">
      <pre class="code">
		{ "ticker": "BTCUSDT", "type": "close-short"}
      </pre>
    </b-form-group>
  </div>
</template>

<script>
export default {
  props: {
    botKey: {
      type: String,
    },
  },
};
</script>

<style>
.code {
  width: 406px;
  background-color: #f5f5f5;
  border: 1px solid #ccc;
  border-radius: 4px;
  padding-top: 6px;
  padding-left: 12px;
  padding-right: 12px;
  padding-bottom: 6px;
}
</style>
