<template>
	<treeselect
		v-model="customValue"
		ref="select"
		:placeholder="placeholder"
		:limit="limit"
		:searchable="searchable"
		:disabled="disabled"
		:options="options"
		:default-expand-level="3"
		:required="required"
		:clearable="clearable"
		:multiple="multiple"
		@input="updateValue"
	>
		<template
			v-for="slot in Object.keys($scopedSlots)"
			:slot="slot"
			slot-scope="scope"
		>
			<slot :name="slot" v-bind="scope" />
		</template>
		<template slot="option-label" slot-scope="{ node }">
			<slot name="option-label" v-bind="{ node }" />
		</template>
	</treeselect>
</template>

<script>
import Treeselect from "@riophae/vue-treeselect";
/**
 * MonoSign Single Select Component
 */
export default {
	name: "Select",
	components: {
		Treeselect,
	},
	props: {
		searchable: {
			type: Boolean,
			default: true,
		},
		/**
		 * Placeholder text.
		 * @type String
		 */
		placeholder: {
			type: String,
			default: "Select...",
			required: false,
		},
		/**
		 * Result limit.
		 * @type Number
		 */
		limit: {
			type: Number,
			default: 10,
			required: false,
		},
		/**
		 * Option list.
		 * @type Array
		 */
		options: {
			type: Array,
			required: true,
		},
		/**
		 * Selected ID
		 * @type String
		 */
		value: {
			required: true,
		},
		required: {
			default: false,
			required: false,
		},
		clearable: {
			default: true,
			required: false,
		},
		/**
		 * Disable component when value is true.
		 * @type Boolean
		 */
		disabled: {
			type: Boolean,
			default: false,
			required: false,
		},
		/**
		 * Whether to clear the search input after selecting an option.
		 */
		clearOnSelect: {
			type: Boolean,
			default: false,
			required: false,
		},
		/**
		 * The method triggered when any value is selected in dropdown.
		 * @event onChange
		 */
		onChange: {
			type: Function,
			required: false,
		},
		multiple: {
			type: Boolean,
			default: false,
		},
		showSelectAll: {
			type: Boolean,
			default: false,
		},
	},
	watch: {
		/*
    options(val) {
      this.customOptions = val;
    },
    */
		value(val) {
			this.customValue = val;
		},
	},
	data() {
		return {
			// customOptions: [],
			customValue: this.value,
		};
	},
	methods: {
		updateValue() {
			this.$emit("input", this.customValue);

			if (this.onChange) {
				this.onChange(this.customValue);
			}
		},
		clear() {
			this.$refs.select.clear();
		},
	},
};
</script>
