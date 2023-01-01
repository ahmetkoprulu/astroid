<template>
  <div class="sticky top-0 z-80 mb-6">
    <div
      class="flex items-center flex-wrap lg:flex-nowrap gap-x-3 lg:gap-4 w-full md:px-0 relative"
    >
      <div class="flex items-center gap-3">
        <p class="font-bold h3 mb-0 mr-6 order-1">{{ title }}</p>
      </div>
      <!-- Right Corner -->
      <div
        class="flex-1 flex items-center justify-end gap-3 py-4 lg:py-5 order-2 lg:order-3"
      >
        <div class="relative">
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
            <b-button
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
            </b-button>
          </span>
        </div>
      </div>
    </div>
    <div class="w-full border-b border-gray-300"></div>
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
<style scoped></style>
