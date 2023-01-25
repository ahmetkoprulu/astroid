<template>
  <b-dropdown
    :variant="variant"
    :size="size"
    :disabled="disabled"
    right
    boundary="window"
  >
    <template v-slot:button-content>
      <i :class="`mr-1 ${icon}`" />{{ label }}
    </template>
    <slot></slot>
  </b-dropdown>
</template>

<script>
export default {
  name: "v-dropdown",
  props: {
    label: {
      type: String,
      required: false,
    },
    icon: {
      required: false,
      default: "",
    },
    size: {
      type: String,
      default: "sm",
      required: false,
      validator: function (value) {
        return ["xs", "sm", "md", "default", "lg"].indexOf(value) !== -1;
      },
    },
    variant: {
      type: String,
      default: "light",
      required: false,
      validator: function (value) {
        return ["light", "default"].indexOf(value) !== -1;
      },
    },
    disabled: {
      type: Boolean,
      default: false,
      required: false,
    },
  },
};
</script>
