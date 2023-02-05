<template>
  <div>
    <div
      class="btn-group-toggle d-inline-block mr-2"
      v-for="opt in options"
      :key="opt.key"
    >
      <label
        class="btn btn-sm btn-outline-primary"
        :class="{
          active: opt.value === value,
          'btn-outline-primary': opt.value === value,
          'btn-outline-secondary': opt.value !== value,
        }"
        style="width: 100px"
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
  },
};
</script>

<style></style>
