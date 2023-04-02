<template>
  <v-select
    :options="dataItems(property.data)"
    v-model="property.value"
    data-vv-name="State"
    v-if="property.type === $consts.PROPERTY_TYPES.Dropdown"
  />
  <v-checkbox
    v-model="property.value"
    label="Yes"
    v-else-if="property.type === $consts.PROPERTY_TYPES.Boolean"
  />

  <!-- <template v-else-if="property.type === $consts.PROPERTY_TYPES.KeyValue">
      <v-key-value-input
        v-model="property.value"
        keyName="SourceProperty"
        keyTitle="Source Property"
        valueName="ProfileProperty"
        valueTitle="Profile Property"
      />
    </template> -->

  <!-- <template v-else-if="property.type === $consts.PROPERTY_TYPES.Json">
      <div>
        <textarea
          class="form-control text-monospace"
          rows="8"
          v-model="property.value"
          @change="beautify(property)"
        />
        <button class="btn btn-xs btn-light mt-2" @click="beautify(property)">
          <i class="fa-light fa-brackets-curly"></i>
          Beautify
        </button>
      </div>
    </template> -->

  <input
    type="password"
    class="form-control"
    :id="property.property"
    :name="property.property"
    v-model="property.value"
    v-else-if="property.encrypted"
  />

  <input
    type="text"
    class="form-control"
    :id="property.property"
    :name="property.property"
    v-model="property.value"
    v-else
  />
</template>

<script>
export default {
  name: "DynamicInput",
  props: {
    property: {
      type: Object,
    },

    properties: {
      type: Array,
    },

    source: {
      type: Object,
    },
  },
  methods: {
    dataItems(data) {
      const value = JSON.parse(data);
      return value.map((x) => {
        return { id: x.key, label: x.value };
      });
    },
  },
};
</script>
