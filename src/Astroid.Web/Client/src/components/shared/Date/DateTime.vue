<template>
  <span>
    <template>
      <span
        v-if="$options.filters.valid(value)"
        v-html="display"
        v-b-tooltip="getTooltipOptions()"
      />
      <span v-else v-html="display" />
    </template>
  </span>
</template>

<script>
/**
 * MonoSign Boolean Types UI Component
 * @example ./Docs/Boolean.md
 */
export default {
  name: "DateTime",
  props: {
    /**
     * Date Time Value
     * @type Date
     */
    value: {
      required: false,
    },
    /**
     * Show formatted date time
     * @type Boolean
     */
    pretty: {
      required: false,
      default: false,
    },
    /**
     * Show tooltip
     * @type Boolean
     */
    tooltip: {},
    /**
     * Show tooltip
     * @type Boolean
     */
    tooltipPlacement: {
      required: false,
      default: "right",
    },
    /**
     * Show prefix
     * @type Boolean
     */
    prefix: {
      required: false,
    },
    /**
     * Show placeholder when value is empty.
     * @type String
     */
    placeholder: {
      required: false,
    },
    expirationAlert: {
      required: false,
      type: Boolean,
    },
    invalidText: {
      required: false,
      type: String,
      default: "Never",
    },
    type: {
      required: false,
      default: "datetime",
    },
  },
  methods: {
    getTooltipOptions() {
      if (typeof this.tooltip === "undefined") return {};
      const title = this.$options.filters.format(this.value);
      const placement = this.tooltipPlacement;
      return {
        title: title,
        placement: placement,
        animation: false,
        variant: "secondary",
        delay: {
          show: 500,
          hide: 0,
        },
      };
    },
  },
  computed: {
    display() {
      let value = this.value;
      if (this.pretty !== false) {
        value = this.$options.filters.pretty(value, this.placeholder);
      } else {
        if (this.$options.filters.valid(value)) {
          if (this.type == "date")
            value = this.$options.filters.format(value, "DD/MM/YYYY");
          else value = this.$options.filters.format(value);
        } else {
          value = this.placeholder != null ? this.placeholder : "never";
        }
      }

      if (this.expirationAlert && value != "never") {
        var currentDateString = this.$options.filters.format(Date.now());
        var formattedDate = currentDateString.substring(0, 10).split("/");
        let currentDate = new Date(
          formattedDate[2],
          formattedDate[1],
          formattedDate[0]
        );

        var formattedExDate = value.substring(0, 10).split("/");
        let expirationDate = new Date(
          formattedExDate[2],
          formattedExDate[1],
          formattedExDate[0]
        );

        if (expirationDate < currentDate) {
          value =
            '<span class="fas" style="color:#e74c3c"><i class="fas fa-exclamation mr-1"></i>' +
            value +
            "</spanv>";
        }
      }
      return this.$options.filters.prefix(value, this.prefix || "", "");
    },
  },
};
</script>
