<template>
  <div>
    <page-header title="Save Market" :actions="actions" />
    <div class="col-lg-5 col-md-12 mx-md-3">
      <ValidationObserver ref="form">
        <v-validated-input label="Label">
          <b-form-input type="text" v-model="model.name" />
        </v-validated-input>
        <b-form-group label="Description">
          <b-form-textarea v-model="model.description" rows="2" max-rows="6" />
        </b-form-group>
        <v-validated-input label="Market">
          <v-select
            v-model="model.providerId"
            :options="providerOptions"
            placeholder="Select a market"
            @input="onSelect"
            v-if="!id"
          />
          <span v-else> {{ model.providerName }}</span>
        </v-validated-input>
        <v-validated-input
          :label="property.displayName"
          :description="property.description"
          v-for="property in this.model.properties"
          :key="property.property"
          :rules="property.required ? 'required' : ''"
        >
          <v-dynamic-input v-model="property.value" :property="property" />
        </v-validated-input>
      </ValidationObserver>
    </div>
  </div>
</template>

<script>
import Service from "../../services/markets";

export default {
  data() {
    return {
      actions: [
        {
          title: "Delete",
          event: () => this.delete(),
          icon: "fas fa-trash",
          variant: "light",
          hidden: () => !this.id,
        },
        {
          title: "Save",
          event: (b) => this.save(b),
          icon: "fas fa-plus",
          variant: "primary",
        },
      ],
      model: {
        name: null,
        description: null,
        providerId: null,
        properties: [],
      },
      id: null,
      providers: [],
    };
  },
  computed: {
    providerOptions() {
      return this.providers.map((provider) => {
        return {
          id: provider.id,
          label: provider.title,
        };
      });
    },
  },
  async mounted() {
    this.$busy = true;
    this.id = this.$route.params.id;
    if (this.id) {
      const response = await Service.get(this.id);
      this.model = response.data.data;
    } else {
      await this.getMarketProviders();
    }
    this.$busy = false;
  },
  methods: {
    async getMarketProviders() {
      const response = await Service.getProviders();
      this.providers = response.data.data;
    },
    async save(b) {
      const isValid = await this.$refs.form.validate();
      if (!isValid) return;

      b.setBusy(true);
      try {
        await Service.save(this.model);
        this.$router.push({ name: "market-list" });
      } catch (error) {
        console.error(error);
      } finally {
        b.setBusy(false);
      }
    },
    delete() {
      this.$alert.remove(
        "Delete the Market?",
        "You won't be able to undo it",
        async () => {
          try {
            await Service.delete(this.id);
            this.$router.push({ name: "market-list" });
          } catch (error) {
            console.error(error);
          }
        }
      );
    },
    onSelect(id) {
      const provider = this.providers.find((x) => x.id === id);
      this.model.properties = Object.assign(
        this.model.properties,
        provider.properties
      );
    },
  },
};
</script>

<style></style>
