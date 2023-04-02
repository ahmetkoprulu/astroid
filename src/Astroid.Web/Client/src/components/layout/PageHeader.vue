<template>
  <div class="sticky page-header mb-4">
    <div class="">
      <p class="font-bold h3 mb-0 mr-6 order-1">{{ title }}</p>
    </div>
    <!-- Right Corner -->
    <div class="">
      <span
        v-for="(action, index) in filteredActions"
        :key="'btn-action-' + index"
      >
        <router-link
          v-if="action.url"
          :to="action.url"
          v-b-tooltip="action.tooltip"
          :class="`ml-2 button ${action.variant}`"
        >
          <i class="mr-2" :class="action.icon"></i>
          {{ getValue(action.title) }}
        </router-link>
        <v-button
          v-else
          class="ml-2 header-item"
          :variant="action.variant || buttonType"
          @click="action.event"
          v-b-tooltip="action.tooltip"
          :loading="getValue(action.loading)"
          :disabled="getValue(action.loading)"
        >
          <i class="mr-2" :class="action.icon"></i>
          {{ getValue(action.title) }}
        </v-button>
      </span>
    </div>
  </div>
</template>

<script>
export default {
  props: {
    buttonType: {
      type: String,
      default: "primary",
    },
    title: {
      type: String,
      default: document.title,
    },
    actions: {
      type: Array,
      default: () => [],
    },
    loading: {
      type: Boolean,
      default: false,
    },
  },
  methods: {
    getValue(value) {
      if (typeof value === "function") {
        return value();
      }
      return value;
    },
  },
  computed: {
    filteredActions() {
      if (this.loading) return [];
      const filtered = this.actions.filter((x) => {
        let hidden = false;
        if (typeof x.hidden === "function") {
          hidden = x.hidden();
        } else if (x.hidden) {
          hidden = x.hidden;
        }
        return !hidden;
      });
      return filtered;
    },
  },
};
</script>
<style scoped>
.sticky {
  position: sticky;
  top: 0;
  z-index: 100;
  background-color: #fff;
}

.page-header {
  background-color: #fff;
  box-shadow: 0 4px 2px -2px rgba(0, 0, 0, 0.1);
  padding-top: 24px;
  padding-bottom: 24px;
  padding-left: 1.5rem;
  padding-right: 1.5rem;
  display: flex;
  align-items: center;
  justify-content: space-between;
}
</style>
