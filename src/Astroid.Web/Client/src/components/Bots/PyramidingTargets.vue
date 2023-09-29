<template>
	<div>
		<div class="text-center text-muted" v-if="value.length == 0">
			No target exists. Click
			<a href="javascript:;" class="link text-primary" @click="add">here</a> to
			add one.
		</div>
		<div class="row pt-0 pb-2" v-else>
			<div class="col-md-5">{{ quantityLabel }}</div>
			<div class="col-md-5">{{ targetLabel }}</div>
			<span
				class="col-md-1 remove-button"
				@click="
					value.push({
						activation: null,
						share: null,
					})
				"
			>
				<a class="text-primary" href="javascript:;">
					<i class="fas fa-plus fa-fw" />
				</a>
			</span>
		</div>
		<div class="row d-flex mb-2" v-for="(val, i) of value" :key="i">
			<div class="col-md-5">
				<b-form-input type="number" v-model="val.quantity" />
			</div>
			<div class="col-md-5">
				<b-form-input type="number" v-model="val.target" />
			</div>
			<div class="col-md-1 pt-2 remove-button" @click="remove(val)"></div>
			<div class="col-md-1 pt-2 remove-button" @click="remove(val)">
				<a href="javascript:;" class="text-primary">
					<i class="fa-solid fa-minus fa-fw" />
				</a>
			</div>
		</div>
	</div>
</template>

<script>
export default {
	props: {
		value: {
			type: Array,
			required: true,
		},
		quantityLabel: {
			type: String,
			default: "Quantity",
		},
		targetLabel: {
			type: String,
			default: "Target(%)",
		},
	},
	data() {
		return {};
	},
	methods: {
		add() {
			this.value.push({ quantity: null, target: null });
		},
		remove(v) {
			this.value.splice(this.value.indexOf(v), 1);
		},
	},
};
</script>

<style scoped>
.remove-button {
	cursor: pointer;
}
</style>
