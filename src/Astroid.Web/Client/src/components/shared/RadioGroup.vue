<template>
	<div
		class="d-flex flex-wrap w-100"
		:class="`justify-content-${justify}`"
		:style="{ gap: `${gap}px` }"
	>
		<div
			class="btn-group-toggle radio-group-flex"
			:class="{ 'flex-fill': fill, disabled: disabled }"
			v-for="opt in options"
			:key="opt.key"
			:disable="disabled"
		>
			<label
				class="btn btn-outline-primary"
				:class="{
					active: opt.value === value,
					'btn-outline-primary': opt.value === value,
					'btn-outline-secondary': opt.value !== value,
					'btn-sm': size === 'sm',
					'btn-lg': size === 'lg',
				}"
				:style="{ width: width }"
			>
				<input
					v-model="value"
					type="radio"
					class
					:value="opt.value"
					@input="$emit('input', opt.value)"
					:disabled="opt.disabled"
				/>
				<i :class="opt.icon" /> {{ opt.text }}
			</label>
		</div>
	</div>
</template>

<script>
export default {
	computed: {},
	props: {
		value: {
			default: null,
		},
		options: {
			type: Array,
			default: () => [],
		},
		variant: {
			type: String,
			default: "outline-secondary",
		},
		size: {
			type: String,
			default: "sm",
		},
		width: {
			type: String,
			default: "100px",
		},
		justify: {
			type: String,
			default: "start",
		},
		gap: {
			type: Number,
			default: 5,
		},
		fill: {
			type: Boolean,
			default: false,
		},
		disabled: {
			type: Boolean,
			default: false,
		},
	},
};
</script>

<style>
.radio-group-flex {
	flex: 0;
}
</style>
