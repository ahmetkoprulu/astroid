<template>
  <div>
    <page-header title="Markets" :actions="actions" />
    <!-- card shadow-sm -->
    <div class="">
      <v-table
        :columns="columns"
        :requestFunction="requestFunction"
        :refreshButton="false"
        ref="table"
      >
        <template #column-name="props" cols="2">
          <router-link
            :to="{ name: 'market-save', params: { id: props.row.id } }"
          >
            {{ props.row.name }}
          </router-link>
        </template>
        <template #column-createdDate="props" cols="2">
          <v-datetime v-model="props.row.createdDate" />
        </template>
        <template #column-actions="props" cols="1">
          <b-button size="xs" @click="$refs.auditDetails.show(props.row)">
            <mui-icon icon="details" />
            Details
          </b-button>
        </template>
      </v-table>
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
          title: "Create Market",
          url: this.$router.resolve({ name: "market-save" }).href,
          icon: "fa-regular fa-plus",
          variant: "primary",
        },
      ],
      columns: {
        name: "Label",
        providerName: "Market",
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
