<template>
  <b-modal ref="modal" title="Audit Details" ok-only @hidden="hidden">
    <b-form-group label="Type">
      {{ $consts.AUDIT_TYPE_DESCRIPTIONS[audit.type] }}
    </b-form-group>
    <b-form-group label="Description">
      {{ audit.description }}
    </b-form-group>
    <b-form-group :label="d.label" v-for="d in beautifulData" :key="d.label">
      {{ d.value }}
    </b-form-group>
  </b-modal>
</template>
<script>
export default {
  data() {
    return {
      audit: {},
    };
  },
  computed: {
    beautifulData() {
      if (!this.audit) return [];
      if (!this.audit.data) return [];
      let data = JSON.parse(this.audit.data);
      return Object.entries(data).map(([key, value]) => {
        var label = this.$helpers.pascalCaseToTitleCase(key);
        return {
          label: label,
          value: value,
        };
      });
    },
  },
  methods: {
    show(audit) {
      this.audit = audit;
      this.$refs.modal.show();
    },
    hide() {
      this.$refs.modal.hide();
    },
    hidden() {
      this.audit = {};
    },
  },
};
</script>
