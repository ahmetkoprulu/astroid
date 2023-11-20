<template>
	<div>
		<b-form-group label="Webhook">
			{{ `https://trade.ahmetkoprulu.com/api/bots/execute` }}
		</b-form-group>
		<b-form-group label="Open Orders">
			<pre class="code col-12 col-md-12 col-lg-8">

{ 
  "ticker": "BTCUSDT", 
  "leverage": 20, 
  "risk": "1 | 2 | ... | n", 
  "type": "open-long | open-short", 
  "quantity": 100 (in unit of position size),
  "key": "{{ botKey }}" 
}
      </pre>
		</b-form-group>
		<b-form-group label="Close Orders">
			<pre class="code col-12 col-md-12 col-lg-8">

{ 
  "ticker": "BTCUSDT", 
  "type": "close-long | close-short | close-all", 
  "key": "{{ botKey }}" 
}
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
	background-color: var(--md-sys-color-secondary-container-2);
	border: 1px solid var(--md-sys-color-outline);
	color: var(--md-sys-color-on-secondary-container);
	border-radius: 4px;
	padding-top: 6px;
	padding-left: 12px;
	padding-right: 12px;
	padding-bottom: 6px;
}
</style>
