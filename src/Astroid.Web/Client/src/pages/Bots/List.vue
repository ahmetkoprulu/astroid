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
        <template #column-actions="props" cols="2">
          <v-dropdown class="pull-right">
            <v-dropdown-item
              @click="
                () =>
                  $router.push({
                    name: 'bot-save',
                    params: { id: props.row.id },
                  })
              "
            >
              <i class="fa-solid fa-pen-to-square"></i> Edit
            </v-dropdown-item>
            <v-dropdown-item
              @click="
                () =>
                  $router.push({
                    name: 'bot-audits',
                    params: { id: props.row.id },
                  })
              "
            >
              <i class="fa-regular fa-file-lines" /> Audits
            </v-dropdown-item>
            <v-dropdown-divider />
            <v-dropdown-item @click="remove(props.row.id)">
              <span class="text-danger">
                <i class="mr-1 fa-solid fa-trash" /> Delete
              </span>
            </v-dropdown-item>
          </v-dropdown>
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
        actions: " ",
      },
    };
  },
  methods: {
    async requestFunction(filters, sorts, currentPage, perPage) {
      return await Service.list(filters, sorts, currentPage, perPage);
    },
    async remove(id) {
      console.log(id);
    },
  },
};
</script>

<style></style>
