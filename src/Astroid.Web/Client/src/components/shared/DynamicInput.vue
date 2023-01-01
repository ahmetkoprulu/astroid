<template>
  <ValidationProvider
    :rules="property.isRequired ? 'required' : ''"
    v-slot="{ errors }"
    :name="property.title"
  >
    <template v-if="property.type === $consts.propertyTypes.Dropdown">
      <v-select
        :options="dataItems(property.data)"
        v-model="property.value"
        data-vv-name="State"
      />
    </template>

    <template v-else-if="property.type === $consts.propertyTypes.Boolean">
      <v-checkbox v-model="property.value" label="Yes" />
    </template>

    <!-- <template v-else-if="property.type === $consts.propertyTypes.KeyValue">
      <v-key-value-input
        v-model="property.value"
        keyName="SourceProperty"
        keyTitle="Source Property"
        valueName="ProfileProperty"
        valueTitle="Profile Property"
      />
    </template> -->

    <!-- <template v-else-if="property.type === $consts.propertyTypes.Json">
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

    <template v-else-if="property.isEncrypted">
      <input
        type="password"
        class="form-control"
        :id="property.property"
        :name="property.property"
        v-model="property.value"
      />
    </template>

    <template v-else>
      <input
        type="text"
        class="form-control"
        :id="property.property"
        :name="property.property"
        v-model="property.value"
      />
    </template>

    <span class="invalid-feedback d-block" v-if="errors.length > 0">{{
      errors[0]
    }}</span>
  </ValidationProvider>
</template>

<script>
export default {
  name: "DynamicInput",

  components: {},

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
