<template>
  <div class="custom-control custom-checkbox">
    <input
      type="checkbox"
      v-model="checkValue"
      :disabled="disabled"
      :id="elementName"
      :name="elementName"
      @change="emitValue"
      class="custom-control-input"
    />
    <label class="custom-control-label" v-if="label" :for="elementName">
      {{ label }}
    </label>
  </div>
</template>

<script>
/**
 * MonoSign Checkbox Component
 */
export default {
  name: "Checkbox",
  props: {
    /**
     * Checkbox check state value.
     * @model
     * @type Boolean
     */
    value: {
      required: true,
      default: false,
    },
    /**
     * Label text.
     * @type String
     */
    label: {
      value: String,
      required: false,
      default: "Yes",
    },
    /**
     * Label text.
     * @type String
     */
    name: {
      value: String,
      required: false,
    },
    /**
     * Disable component when value is true.
     * @type Boolean
     */
    disabled: {
      type: Boolean,
      default: false,
    },
  },
  computed: {
    elementName() {
      if (this.name) {
        return this.name;
      }
      return `check-${this.$helpers.guid()}`;
    },
  },
  watch: {
    value(val) {
      this.checkValue = val;
    },
  },
  data() {
    let v = false;
    if (typeof this.value === "string") {
      v = this.value.toLowerCase() === "true";
    } else {
      v = this.value === true;
    }
    return {
      checkValue: v,
    };
  },
  methods: {
    emitValue(val) {
      this.$emit("input", val.target.checked);
    },
  },
};
</script>
