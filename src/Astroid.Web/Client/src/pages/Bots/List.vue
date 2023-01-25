<template>
  <div>
    <page-header title="Bots" :actions="actions" />
    <!-- card shadow-sm -->
    <div class="">
      <v-table
        ref="table"
        :columns="columns"
        :requestFunction="requestFunction"
        :refreshButton="false"
      >
        <template #column-label="props" cols="2">
          <router-link :to="{ name: 'bot-save', params: { id: props.row.id } }">
            {{ props.row.label }}
          </router-link>
        </template>
        <template #column-createdDate="props" cols="2">
          <v-datetime v-model="props.row.createdDate" />
        </template>
      </v-table>
    </div>
  </div>
</template>

<script>
import Service from "../../services/bots";
export default {
  data() {
    return {
      actions: [
        {
          title: "Create Bot",
          url: this.$router.resolve({ name: "bot-save" }).href,
          icon: "fa-regular fa-plus",
          variant: "primary",
        },
      ],
      columns: {
        label: "Label",
        createdDate: "Created Date",
      },
    };
  },
  methods: {
    async requestFunction(filters, sorts, currentPage, perPage) {
      return await Service.list(filters, sorts, currentPage, perPage);
    },
  },
};
</script>

<style></style>
