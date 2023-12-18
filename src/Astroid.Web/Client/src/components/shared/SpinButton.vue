<template>
	<b-form-spinbutton
		placeholder="Leverage"
		v-model="value"
		@input="$emit('input', $event)"
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
</template>
<script>
export default {
	name: "SpinButton",
	props: {
		value: {
			type: Number,
			default: 0,
		},
	},
};
</script>
