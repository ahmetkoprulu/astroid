<template>
	<b-dropdown
		variant="default"
		:split="split"
		size="sm"
		@click="click"
		boundary="window"
	>
		<template v-slot:button-content>
			<i
				v-if="SelectedOption.icon"
				:class="`mr-1 fa-fw ${SelectedOption.icon}`"
			/>
			{{ SelectedOption.title }}
		</template>
		<b-dropdown-item
			v-for="(option, index) in options"
			:key="`${name} ${index}`"
			@click="changed(option.id)"
			:active="value === option.id"
		>
			<i v-if="SelectedOption.icon" :class="`mr-1 fa-fw ${option.icon}`" />
			{{ option.title }}
		</b-dropdown-item>
	</b-dropdown>
</template>

<script>
export default {
	name: "dropdown-select",
	props: {
		name: {
			type: String,
			default: "dropdown-select",
		},
		value: {
			type: Number,
			default: 1,
		},
		options: {
			type: Array,
			default: () => [],
		},
		iconOnly: {
			type: Boolean,
			default: false,
		},
		split: {
			type: Boolean,
			default: false,
		},
	},
	computed: {
		SelectedOption() {
			var o = this.options.find((x) => x.id === this.value);
			if (o) return o;

			return {};
		},
	},
	methods: {
		changed(value) {
			this.$emit("changed", value);
			this.$emit("input", value);
		},
		click() {
			this.$emit("click", this.val);
		},
	},
};
</script>
