<template>
  <div>
    <page-header title="Audits" />
    <!-- card shadow-sm -->
    <div class="">
      <v-table
        ref="table"
        :columns="columns"
        :filters="filters"
        :requestFunction="requestFunction"
        :refreshButton="false"
      >
        <template #column-type="props" cols="2">
          {{ $consts.AUDIT_TYPE_DESCRIPTIONS[props.row.type] }}
        </template>
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
            <v-dropdown-item @click="showDetails(props.row)">
              <i class="mr-1 fa-solid fa-circle-info" /> Show Details
            </v-dropdown-item>
          </v-dropdown>
        </template>
      </v-table>
    </div>
    <DetailModal ref="detailModal" />
  </div>
</template>

<script>
import Service from "../../services/audits";
import DetailModal from "@/components/Audit/AuditDetailsModal";

export default {
  data() {
    return {
      columns: {
        type: "Type",
        description: "Description",
        createdDate: "Created Date",
        correlationId: "Correlation",
        actions: " ",
      },
      botId: null,
      filters: [],
    };
  },
  created() {
    this.botId = this.$route.params.id;
  },
  methods: {
    async requestFunction(filters, sorts, currentPage, perPage) {
      return await Service.list(
        this.botId,
        filters,
        sorts,
        currentPage,
        perPage
      );
    },
    showDetails(audit) {
      this.$refs.detailModal.show(audit);
    },
  },
  components: {
    DetailModal,
  },
};
</script>

<style></style>
