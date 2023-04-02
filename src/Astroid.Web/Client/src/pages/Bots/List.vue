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
        <template #column-label="props">
          <router-link :to="{ name: 'bot-save', params: { id: props.row.id } }">
            {{ props.row.label }}
          </router-link>
        </template>
        <template #column-createdDate="props">
          <v-datetime v-model="props.row.createdDate" />
        </template>
        <template #column-actions="props">
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
              <i class="fa-solid fa-pen-to-square" /> Edit
            </v-dropdown-item>
            <v-dropdown-item @click="showChangeMarginTypeModal(props.row.id)">
              <i class="fa-solid fa-shuffle" /> Change Margin Type
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
            <v-dropdown-item @click="deleteBot(props.row.id)">
              <span class="text-danger">
                <i class="mr-1 fa-solid fa-trash" /> Delete
              </span>
            </v-dropdown-item>
          </v-dropdown>
        </template>
      </v-table>
    </div>
    <ChangeMarginTypeModal ref="changeMarginTypeModal" />
  </div>
</template>

<script>
import Service from "../../services/bots";
import ChangeMarginTypeModal from "../../components/Bots/ChangeMarginTypeModal";

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
    deleteBot(id) {
      this.$alert.remove(
        "Delete the Bot?",
        "You won't be able to undo it",
        async () => {
          try {
            const response = await Service.delete(id);
            if (!response.data.success) {
              this.$errorToast("Delete Bot", response.data.message);
              return;
            }

            this.$successToast("Delete Bot", "Bot deleted successfully");
            this.$refs.table.refresh();
          } catch (error) {
            this.$errorToast("Delete Bot", error.message);
          }
        }
      );
    },
    async remove(id) {
      console.log(id);
    },
    async showChangeMarginTypeModal(id) {
      this.$refs.changeMarginTypeModal.show(id);
    },
  },
  components: {
    ChangeMarginTypeModal,
  },
};
</script>

<style></style>
