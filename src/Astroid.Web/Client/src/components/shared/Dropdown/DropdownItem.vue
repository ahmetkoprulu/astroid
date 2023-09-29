<template>
	<b-dropdown-item @click.stop="onClick">
		<slot></slot>
	</b-dropdown-item>
</template>

<script>
export default {
	name: "v-dropdown-item",
	props: {},
	methods: {
		onClick() {
			this.$emit("click");
		},
	},
};
</script>
