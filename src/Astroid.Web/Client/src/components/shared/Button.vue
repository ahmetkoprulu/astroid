<template>
  <b-button v-bind="$attrs" @click.stop.prevent="onClick" :disabled="busy">
    <!-- <b-spinner class="mr-1" small v-if="busy" /> -->
    <slot />
  </b-button>
</template>
<script>
export default {
  inheritAttrs: false,
  data() {
    return {
      busy: false,
    };
  },
  methods: {
    onClick() {
      this.$emit("click", { setBusy: this.setBusy });
    },
    setBusy(busy) {
      this.busy = busy;
    },
  },
};
</script>
