<template>
  <vue-tel-input
    v-model="number"
    @input="onInput"
    styleClasses="form-control phone-input"
  />
</template>
<script>
export default {
  name: "PhoneInput",
  props: {
    value: {
      type: String,
    },
  },
  data() {
    return {
      number: null,
    };
  },
  watch: {
    value(v) {
      this.number = v;
    },
  },
  methods: {
    onInput(_, phone) {
      if (!phone.number && phone.formatted) return;

      if (!phone.number) phone.number = "";
      this.$emit("input", phone.number);
    },
  },
};
</script>
<style>
.vti__search_box {
  border: 1px solid #ced4da !important;
  border-radius: 4px !important;
  width: 92% !important;
  padding: 7px;
  margin: 8px 0px 8px 16px !important;
}

.vti__dropdown-list.below {
  font-weight: 400 !important;
  top: 43px !important;
}
.vti__dropdown-list {
  overflow-x: hidden;
}

.phone-input {
  font-size: 1rem !important;
  color: #495057 !important;
  border: 1px solid #ced4da !important;
  padding: 0 !important;
}

.vti__input {
  color: #495057 !important;
}

.vue-tel-input:focus-within {
  box-shadow: 0 0 0 0.2rem rgb(0 123 255 / 25%) !important;
}

.vti__flag-wrapper > strong {
  font-size: 5rem;
  font-weight: normal !important;
}

strong {
  font-weight: normal;
}
</style>
