<template>
  <div>
    <div class="text-center text-muted" v-if="value.length == 0">
      No target exists. Click
      <a href="javascript:;" class="link text-primary" @click="add">here</a> to
      add one.
    </div>
    <div class="d-flex mb-2" v-for="(val, i) of value" :key="i">
      <b-form-input
        class="mr-2"
        type="number"
        :placeholder="`Profit Target ${i + 1}`"
        v-model="val.activation"
      />
      <b-form-input
        class="mr-2"
        type="number"
        placeholder="Share"
        v-model="val.share"
      />
      <span class="pt-2 remove-button" @click="remove(val)">
        <a href="javascript:;">
          <i class="fa-solid fa-minus fa-fw" />
        </a>
      </span>
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
  },
  data() {
    return {};
  },
  methods: {
    add() {
      this.value.push({ activation: null, share: null });
    },
    remove(v) {
      this.value.splice(this.value.indexOf(v), 1);
    },
  },
};
</script>

<style>
.remove-button {
  cursor: pointer;
}
</style>
