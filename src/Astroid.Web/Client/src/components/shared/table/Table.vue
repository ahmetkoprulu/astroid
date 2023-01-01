<template>
  <div>
    <div
      :class="{
        'table-responsive': isResponsive,
        'table-relative': isLoading,
      }"
      :style="tableStyle"
    >
      <div
        v-if="isLoading && !isEmpty"
        style="position: absolute; width: 100%; height: 100%; left: 0; top: 0"
      >
        <div
          class="d-flex align-items-center justify-content-center w-100 h-100"
        >
          <b-spinner variant="primary" />
        </div>
        <div
          style="
            background: #ddd;
            opacity: 0.4;
            position: absolute;
            width: 100%;
            height: 100%;
            left: 0;
            top: 0;
          "
        />
      </div>
      <table
        :class="`table table-hover table-condensed mt-0 w-100 d-block d-md-table flex-grow-1 ${tableClass}`"
      >
        <thead :class="`${headerClass} w-100`">
          <tr>
            <slot name="header" v-bind="columns">
              <template
                v-for="(columnValue, columnKey, indexColumn) in columns"
              >
                <template>
                  <th
                    :key="indexColumn"
                    :class="{
                      'has-sort': isSortable(columnKey),
                      'has-sorted-th': isSorted(columnKey),
                    }"
                    @click="toggleSort(columnKey)"
                  >
                    <slot
                      :name="`header-${columnKey}`"
                      v-bind="{
                        key: columnKey,
                        value: columnValue,
                        index: indexColumn,
                      }"
                    >
                      {{ columnValue }}
                      <small
                        v-if="showColumnSubHeader"
                        class="d-block text-muted"
                      >
                        {{ columnKey }}
                      </small>
                      <span v-if="isSortable(columnKey)" class="sort-position">
                        <component
                          :is="sortableIcon(columnKey)"
                          :class="sortableIconClass(columnKey)"
                        />
                      </span>
                    </slot>
                  </th>
                </template>
              </template>
            </slot>
          </tr>
          <template v-if="filterable">
            <tr>
              <th
                class="p-0 p-t-1 p-b-1"
                :colspan="Object.keys(columns).length"
              >
                <b-form-input
                  type="search"
                  placeholder="Search"
                  v-model="searchWord"
                />
              </th>
            </tr>
          </template>
        </thead>
        <tbody>
          <tr class="text-center" v-if="isDataEmpty">
            <td :colspan="Object.keys(columns).length">
              <slot name="empty-data">
                <span> {{ emptyMessage }}</span>
              </slot>
            </td>
          </tr>
          <template v-if="isLoading && isEmpty">
            <tr v-for="i of settings.itemPerPage" :key="'row-' + i">
              <td
                v-for="col in Object.keys(columns)"
                :key="'col-' + col"
                class="text-center"
              >
                <div
                  :style="{
                    opacity: (10 - i) * 0.15,
                  }"
                >
                  <slot :name="'loading-' + col">
                    <b-skeleton width="70%" />
                  </slot>
                </div>
              </td>
            </tr>
          </template>
          <slot name="body" v-bind="tableData" v-if="!isDataEmpty">
            <slot
              name="row-base"
              v-bind="{ row: row, index: rowIndex }"
              v-for="(row, rowIndex) in tableData"
            >
              <tr :key="rowIndex">
                <slot name="row" v-bind="{ row: row, index: rowIndex }">
                  <template
                    v-for="(columnValue, columnKey, indexColumn) in columns"
                  >
                    <template>
                      <slot
                        :name="`column-base-${columnKey}`"
                        v-bind="{
                          row: row,
                          index: rowIndex,
                        }"
                      >
                        <td
                          :key="indexColumn"
                          :class="{
                            'has-sorted-td': isSorted(columnKey),
                          }"
                        >
                          <template>
                            <slot
                              :name="`column-${columnKey}`"
                              v-bind="{
                                row: row,
                                index: rowIndex,
                              }"
                            >
                              {{ getRowValue(row, columnKey) }}
                            </slot>
                          </template>
                        </td>
                      </slot>
                    </template>
                  </template>
                </slot>
              </tr>
            </slot>
          </slot>
        </tbody>
      </table>
    </div>
    <slot name="pagination-seperator" />
    <slot name="pagination">
      <div :class="paginationClass">
        <div class="d-flex justify-content-between align-items-center mt-2">
          <slot name="pagination-filters">
            <div>
              <div v-show="!isDataEmpty">
                <span>Show</span>
                <b-dropdown
                  :text="itemPerPageText"
                  class="m-md-2"
                  size="sm"
                  variant="light"
                >
                  <template v-for="perPage in filteredPerPageValues">
                    <b-dropdown-item
                      :key="'per-page-' + perPage"
                      @click="changeItemPerPage(perPage)"
                    >
                      {{ perPage }}
                    </b-dropdown-item>
                  </template>
                  <b-dropdown-divider
                    v-if="
                      filteredPerPageValues &&
                      filteredPerPageValues.length &&
                      inLimit()
                    "
                  />
                  <template>
                    <b-dropdown-item
                      v-if="inLimit()"
                      @click="changeItemPerPage(settings.totalItemCount)"
                    >
                      All ({{ settings.totalItemCount }})
                    </b-dropdown-item>
                  </template>
                </b-dropdown>
              </div>
            </div>
          </slot>
          <slot name="pagination-center" v-bind="settings">
            <div>
              <span v-show="settings.totalItemCount && !refreshButton">
                {{ settings.totalItemCount }} rows
              </span>
              <b-button
                v-if="refreshButton"
                variant="light"
                id="tooltip-button-5"
                class="ml-2 mr-2"
                @click="refresh"
              >
                <i class="fas fa-sync"></i>
                Refresh
                <span v-show="settings.totalItemCount">
                  | {{ settings.totalItemCount }} Rows
                </span></b-button
              >
            </div>
          </slot>
          <slot name="pagination-buttons">
            <div>
              <b-pagination
                v-if="settings.pageCount > 1"
                v-model="settings.currentPage"
                :total-rows="settings.totalItemCount"
                :per-page="settings.itemPerPage"
                @change="changePage"
                v-bind="pagination"
              />
            </div>
          </slot>
        </div>
      </div>
    </slot>
    <slot name="footer" />
  </div>
</template>
<style>
.table-relative {
  position: relative;
}
</style>
<script>
import IconArrowUp from "./Icons/ArrowUp.vue";
import IconArrowUpLong from "./Icons/ArrowUpLong.vue";
import IconArrowDown from "./Icons/ArrowDown.vue";
import IconArrowDownLong from "./Icons/ArrowDownLong.vue";
import IconSorting from "./Icons/Sorting.vue";
import IconSortingAZ from "./Icons/SortingAZ.vue";
import IconSortingArrows from "./Icons/SortingArrows.vue";
export default {
  name: "v-table",
  components: {
    IconArrowUp,
    IconArrowUpLong,
    IconArrowDown,
    IconArrowDownLong,
    IconSorting,
    IconSortingAZ,
    IconSortingArrows,
  },
  props: {
    history: {
      type: Boolean,
      required: false,
      default: false,
    },
    /// <summary>
    /// Keep the table navigation history on options: 'query', 'cookie', 'session', 'local', 'hash'.
    /// </summary>
    historyStorage: {
      type: String,
      required: false,
      default: "query",
    },
    historyName: {
      type: String,
      default: "",
    },
    columns: {
      type: Object,
      required: false,
    },
    sortable: {
      type: Array,
      required: false,
    },
    showColumnSubHeader: {
      type: Boolean,
      required: false,
      default: false,
    },
    requestFunction: {
      type: Function,
      required: false,
    },
    filterable: {
      type: Boolean,
      required: false,
      default: false,
    },
    headerClass: {
      type: String,
      required: false,
      default: "",
    },
    tableClass: {
      type: String,
      required: false,
      default: "",
    },
    tableStyle: {
      type: String,
      required: false,
      default: "",
    },
    isResponsive: {
      type: Boolean,
      required: false,
      default: true,
    },
    refreshButton: {
      type: Boolean,
      required: false,
      default: true,
    },
    pagination: {
      type: Object,
      required: false,
      default: () => {
        return { class: "m-0", size: "sm" };
      },
    },
    perPageValues: {
      type: Array,
      required: false,
      default: () => [10, 25, 50, 100, 250],
    },
    emptyMessage: {
      type: String,
      required: false,
      default: "No data available in table",
    },
    autoLoad: {
      type: Boolean,
      required: false,
      default: true,
    },
    filters: {
      type: Array,
      required: false,
      default: () => [],
    },
    sorting: {
      type: Array,
      required: false,
      default: () => [],
    },
    sortingIconName: {
      type: String,
      required: false,
      default: "icon-sorting-a-z",
    },
    sortingIconClass: {
      type: String,
      required: false,
      default: "text-muted",
    },
    sortingIconActiveClass: {
      type: String,
      required: false,
      default: "text-primary",
    },
    sortingIconAscName: {
      type: String,
      required: false,
      default: "icon-arrow-up-long",
    },
    sortingIconDescName: {
      type: String,
      required: false,
      default: "icon-arrow-down-long",
    },
    searchColumn: {
      type: String,
      required: false,
      default: "",
    },
    searchWordMinLength: {
      type: Number,
      required: false,
      default: 2,
    },
    searchDebounce: {
      type: Number,
      required: false,
      default: 300,
    },
    paginationClass: {
      type: String,
      required: false,
      default: "",
    },
    perPage: {
      type: Number,
      required: false,
      default: 10,
    },
  },
  watch: {
    searchWord: {
      handler: function (newValue) {
        if (!this.comingFromPopState) this.addFilter(newValue);
      },
    },
  },
  data() {
    return {
      settings: {
        currentPage: 1,
        pageCount: 0,
        itemPerPage: 10,
        totalItemCount: 0,
      },
      tableData: [],
      searchWord: null,
      isDataEmpty: false,
      isLoading: false,
      limit: 250,
      searchDebounceTimeout: null,
      comingFromPopState: false,
      skipWatch: false,
    };
  },
  computed: {
    filteredPerPageValues() {
      const values = [];
      for (const value of this.perPageValues) {
        if (value <= this.settings.totalItemCount) {
          values.push(value);
        }
      }
      return values;
    },
    itemPerPageText() {
      if (this.settings.itemPerPage >= this.settings.totalItemCount) {
        return "All";
      }
      return `${this.settings.itemPerPage}`;
    },
    isEmpty() {
      return this.settings.totalItemCount <= 0;
    },
  },
  created() {
    this.addPopStateListener();
  },
  async mounted() {
    var filterPrefix = this.getHistoryName("f-");
    var sortingPrefix = this.getHistoryName("s-");
    let url = new URL(window.location).toString();
    if (url.indexOf(filterPrefix) >= 0 || url.indexOf(sortingPrefix) >= 0) {
      this.loadHistory();
    }
    if (this.autoLoad) {
      await this.getValues();
    }
  },
  methods: {
    addPopStateListener() {
      window.addEventListener("popstate", this.popStateEvent);
    },
    removePopStateListener() {
      window.removeEventListener("popstate", this.popStateEvent);
    },
    async popStateEvent() {
      this.comingFromPopState = true;
      this.loadHistory();
      this.getValues(false);
    },
    loadHistory() {
      this.settings.currentPage = this.getHistoryParameter("page", 1);
      this.settings.itemPerPage = this.getHistoryParameter(
        "limit",
        this.perPage || this.settings.itemPerPage
      );
      if (this.searchColumn) {
        this.searchWord = this.getHistoryParameter(
          `f-${this.searchColumn}`,
          ""
        );
      }
      this.loadHistoryFilters();
      this.loadHistorySorting();
    },
    sortableIcon(column) {
      const currentSort = this.sorting.find((x) => x.Column === column);
      if (!currentSort) {
        return this.sortingIconName;
      }
      if (currentSort.Asc) {
        return this.sortingIconAscName;
      } else {
        return this.sortingIconDescName;
      }
    },
    sortableIconClass(column) {
      if (this.isSorted(column)) {
        return this.sortingIconActiveClass;
      }
      return this.sortingIconClass;
    },
    isSorted(column) {
      return this.sorting.some((x) => x.Column === column);
    },
    async toggleSort(column) {
      if (!this.isSortable(column)) return;

      let currentSort = this.sorting.find((x) => x.Column === column);
      if (!currentSort) {
        currentSort = { Column: column, Asc: true };
        this.sorting.push(currentSort);
      } else {
        if (!currentSort.Asc) {
          this.sorting.splice(this.sorting.indexOf(currentSort), 1);
        } else {
          currentSort.Asc = false;
        }
      }

      await this.getValues();
    },
    isSortable(column) {
      if (!this.sortable) return false;
      if (this.sortable.indexOf(column) > -1) return true;
      return false;
    },
    getRowValue(row, key) {
      const splitted = key.split(".");
      return splitted.reduce(
        (obj, key) => (obj && obj[key] !== "undefined" ? obj[key] : undefined),
        row
      );
    },
    async getValues(addHistory = true) {
      this.isLoading = true;
      this.$emit("loading", this.isLoading);
      const { currentPage, itemPerPage } = this.settings;

      let result = await this.requestFunction(
        this.filters,
        this.sorting,
        currentPage,
        itemPerPage
      );

      if (addHistory && !this.comingFromPopState) {
        const filterPrefix = this.getHistoryName("f-");
        const sortPrefix = this.getHistoryName("s-");
        let url = new URL(window.location);
        let params = new URLSearchParams(url.search);
        //console.log("url start: " + url);
        for (const searchParam of params) {
          if (searchParam[0].startsWith(filterPrefix)) {
            url.searchParams.delete(searchParam[0]);
            //console.log("url later: " + url);
          }

          if (searchParam[0].startsWith(sortPrefix)) {
            url.searchParams.delete(searchParam[0]);
            //console.log("url later: " + url);
          }
        }

        for (const filter of this.filters) {
          url = this.addHistoryParameter(
            `f-${filter.Column}`,
            filter.Value,
            this.storage,
            url,
            false
          );
        }

        for (const sort of this.sorting) {
          url = this.addHistoryParameter(
            `s-${sort.Column}`,
            sort.Asc ? 1 : 0,
            this.storage,
            url,
            false
          );
        }
        this.replaceHistory(url);
      }
      this.comingFromPopState = false;
      this.skipWatch = false;

      if (!result || result.status !== 200) {
        this.isLoading = false;
        this.isDataEmpty = true;
        this.$emit("loading", this.isLoading);
        return;
      }

      const resultData = result.data.data ? result.data.data : result.data.Data;

      this.settings.currentPage =
        resultData.currentPage || resultData.CurrentPage;
      this.settings.pageCount = resultData.pageCount || resultData.PageCount;
      this.settings.itemPerPage =
        resultData.itemPerPage || resultData.ItemPerPage;
      this.settings.totalItemCount =
        resultData.totalItemCount || resultData.TotalItemCount;

      this.tableData = resultData.data || resultData.Data;
      if (this.settings.totalItemCount == 0) {
        this.isDataEmpty = true;
      } else {
        this.isDataEmpty = false;
      }

      this.settings.currentPage = currentPage;
      this.settings.itemPerPage = itemPerPage;
      if (this.settings.currentPage <= 0) {
        this.settings.currentPage = 1;
      }
      if (
        this.settings.itemPerPage ===
        (resultData.totalItemCount || resultData.TotalItemCount)
      ) {
        this.settings.pageCount = 1;
      }
      if (this.settings.pageCount <= 0) {
        this.settings.pageCount = 1;
      }
      if (this.settings.currentPage > this.settings.pageCount) {
        this.settings.currentPage = this.settings.pageCount;
        this.changePage(this.settings.currentPage);
        return;
      }
      this.isLoading = false;
      this.$emit("loading", this.isLoading);
    },
    changePage(page) {
      this.settings.currentPage = page;
      this.addHistoryParameter("page", page);
      this.getValues(false);
    },
    changeItemPerPage(value) {
      this.settings.itemPerPage = value;
      this.addHistoryParameter("limit", value);
      this.getValues(false);
    },
    isLessThanTotalItemCount(val) {
      return this.settings.totalItemCount > val;
    },
    /** */
    refresh() {
      if (!this.comingFromPopState) this.getValues();
    },
    addFilter(word) {
      if (!this.searchColumn) return;
      clearTimeout(this.searchDebounceTimeout);
      this.searchDebounceTimeout = setTimeout(
        () =>
          this.searchColumnValue(
            this.searchColumn,
            word,
            this.searchWordMinLength
          ),
        this.searchDebounce
      );
    },
    searchColumnValue(column, value, limit = -1) {
      if (this.skipWatch) return;
      let filter = this.filters.find((x) => x.Column === column);

      if (filter && (!value || value.length < limit)) {
        this.filters.splice(this.filters.indexOf(filter), 1);
        this.getValues();
        return;
      }

      if (filter) {
        filter.Value = value;
      } else {
        this.filters.push({
          Column: column,
          Value: value,
        });
      }
      this.getValues();
    },
    inLimit() {
      return this.settings.totalItemCount <= this.limit;
    },
    getHistoryName(name, seperator = "-") {
      if (!name) name = "";
      if (!this.historyName) return name;
      return `${this.historyName}${seperator}${name}`;
    },
    clearHistoryParameters(startsWith = null, storage = this.historyStorage) {
      if (!startsWith || !storage) return;
      try {
        const startsWithName = this.getHistoryName(startsWith);

        if (storage === "query") {
          const url = new URL(window.location);
          for (const searchParam of url.searchParams) {
            if (searchParam[0].startsWith(startsWithName)) {
              url.searchParams.delete(searchParam[0]);
            }
          }
          return url;
        }

        if (storage === "local") {
          const keys = Object.keys(localStorage);
          for (const key of keys) {
            if (!key.startsWith(startsWithName)) continue;
            localStorage.removeItem(key);
          }
          return;
        }

        if (storage === "session") {
          const keys = Object.keys(sessionStorage);
          for (const key of keys) {
            if (!key.startsWith(startsWithName)) continue;
            sessionStorage.removeItem(key);
          }
          return;
        }

        if (storage === "cookie") {
          var pairs = document.cookie.split(";");
          var cookies = {};
          for (var i = 0; i < pairs.length; i++) {
            var pair = pairs[i].split("=");
            cookies[(pair[0] + "").trim()] = unescape(pair.slice(1).join("="));
          }
          const keys = Object.keys(cookies);
          for (const key of keys) {
            if (!key.startsWith(startsWithName)) continue;
            document.cookie = `${key}=; expires=Thu, 01 Jan 1970 00:00:00 UTC;`;
          }
          return;
        }

        if (storage === "hash") {
          if (!window.location.hash) return;
          const hash = window.location.hash.substr(1);
          hash.split("&").reduce(function (_, item) {
            var parts = item.split("=");
            if (!parts[0].startsWith(startsWithName)) return;
            window.location.hash = window.location.hash.replace(`${item}`, "");
          }, {});
          return;
        }
      } catch (error) {
        console.error(error);
      }
    },
    getHistoryParameters(startsWith = null, storage = this.historyStorage) {
      if (!storage) return;

      try {
        const startsWithName = this.getHistoryName(startsWith);
        let returnParameters = {};

        if (storage === "query") {
          const url = new URL(window.location);
          for (const searchParam of url.searchParams) {
            if (!searchParam[0].startsWith(startsWithName)) continue;
            returnParameters[searchParam[0]] = searchParam[1];
          }
        }

        if (storage === "local") {
          const keys = Object.keys(localStorage);
          for (const key of keys) {
            if (!key.startsWith(startsWithName)) continue;
            returnParameters[key] = localStorage.getItem(key);
          }
        }

        if (storage === "session") {
          const keys = Object.keys(sessionStorage);
          for (const key of keys) {
            if (!key.startsWith(startsWithName)) continue;
            returnParameters[key] = sessionStorage.getItem(key);
          }
        }

        if (storage === "cookie") {
          var pairs = document.cookie.split(";");
          var cookies = {};
          for (var i = 0; i < pairs.length; i++) {
            var pair = pairs[i].split("=");
            cookies[(pair[0] + "").trim()] = unescape(pair.slice(1).join("="));
          }
          const keys = Object.keys(cookies);
          for (const key of keys) {
            if (startsWithName && !key.startsWith(startsWithName)) continue;
            returnParameters[key] = cookies[key];
          }
        }

        if (storage === "hash") {
          if (window.location.hash) {
            const hash = window.location.hash.substr(1);
            hash.split("&").reduce(function (_, item) {
              var parts = item.split("=");
              if (startsWithName && !parts[0].startsWith(startsWithName))
                return;
              returnParameters[parts[0]] = parts[1];
            }, {});
          }
        }

        return returnParameters;
      } catch (error) {
        console.error(error);
      }
    },
    loadHistoryFilters() {
      const parameters = this.getHistoryParameters("f-");
      const trimName = this.getHistoryName("f-");
      const names = Object.keys(parameters);
      this.filters.splice(0, this.filters.length);
      for (const paramName of names) {
        const value = parameters[paramName];
        const column = paramName.replace(trimName, "");

        if (value) {
          this.filters.push({
            Column: column,
            Value: value,
          });
        }
      }
    },
    loadHistorySorting() {
      const parameters = this.getHistoryParameters("s-");
      const trimName = this.getHistoryName("s-");
      const names = Object.keys(parameters);
      this.sorting.splice(0, this.sorting.length);
      for (const paramName of names) {
        var paramValue = parameters[paramName];
        let value = undefined;
        if (paramValue && paramValue === "1") value = true;
        else if (paramValue && paramValue === "0") value = false;
        const column = paramName.replace(trimName, "");

        this.sorting.push({
          Column: column,
          Asc: value,
        });
      }
    },
    replaceHistory(state, storage = this.historyStorage) {
      if (storage === "query") {
        let url;
        if (state) url = state;
        else url = new URL(window.location.origin + window.location.pathname);
        //console.log("Pushing(3): " + url);
        window.history.pushState({ url: url.href }, "", url.href);
        return;
      }
    },
    addHistoryParameter(
      name,
      value,
      storage = this.historyStorage,
      state = null,
      replace = true
    ) {
      if (!this.history || !name || !storage) return;
      try {
        if (storage === "query") {
          let url;
          if (state) url = state;
          else url = new URL(window.location);
          const historyName = this.getHistoryName(name);
          url.searchParams.set(historyName, value);
          if (!replace) {
            state = url;
            return state;
          }
          //console.log("Pushing(2): " + url.href);
          window.history.pushState({ url: url.href }, "", url.href);
          return;
        }

        if (storage === "local") {
          const historyName = this.getHistoryName(name);
          localStorage.setItem(historyName, value);
          return;
        }

        if (storage === "session") {
          const historyName = this.getHistoryName(name);
          sessionStorage.setItem(historyName, value);
          return;
        }

        if (storage === "cookie") {
          const historyName = this.getHistoryName(name);
          document.cookie = `${historyName}=${value}`;
          return;
        }

        if (storage === "hash") {
          const historyName = this.getHistoryName(name);
          const curretParameters = this.getHistoryParameters();
          const newParameters = {
            ...curretParameters,
            [historyName]: value,
          };
          const newHash = Object.keys(newParameters)
            .map((key) => `${key}=${newParameters[key]}`)
            .join("&");
          window.location.hash = newHash;
          return;
        }
      } catch (error) {
        console.error(error);
      }
    },
    getHistoryParameter(
      name,
      defaultValue = null,
      storage = this.historyStorage
    ) {
      if (!this.history || !name || !storage) return defaultValue;

      try {
        if (storage === "query") {
          const url = new URL(window.location);
          const historyName = this.getHistoryName(name);
          return url.searchParams.get(historyName) || defaultValue;
        }

        if (storage === "local") {
          const historyName = this.getHistoryName(name);
          return localStorage.getItem(historyName) || defaultValue;
        }

        if (storage === "session") {
          const historyName = this.getHistoryName(name);
          return sessionStorage.getItem(historyName) || defaultValue;
        }

        if (storage === "cookie") {
          const historyName = this.getHistoryName(name);
          const cookie = document.cookie
            .split(";")
            .find((c) => c.trim().startsWith(`${historyName}=`));
          if (!cookie) return defaultValue;
          return cookie.split("=")[1] || defaultValue;
        }

        if (storage === "hash") {
          const historyName = this.getHistoryName(name);
          const hash = window.location.hash
            .split("#")
            .find((c) => c.trim().startsWith(`${historyName}=`));
          if (!hash) return defaultValue;
          return hash.split("=")[1] || defaultValue;
        }
      } catch (error) {
        console.error(error);
      }

      return defaultValue;
    },
    removeHistoryParameter(name, storage = this.historyStorage) {
      if (!this.history || !name || !storage) return;

      try {
        const historyName = this.getHistoryName(name);
        if (storage === "query") {
          const url = new URL(window.location);
          url.searchParams.delete(historyName);
          //console.log("Pushing(1): " + url);
          window.history.pushState({}, "", url);
          return;
        }

        if (storage === "local") {
          localStorage.removeItem(historyName);
          return;
        }

        if (storage === "session") {
          sessionStorage.removeItem(historyName);
          return;
        }

        if (storage === "cookie") {
          document.cookie = `${historyName}=; expires=Thu, 01 Jan 1970 00:00:00 UTC;`;
          return;
        }

        if (storage === "hash") {
          if (!window.location.hash) return;
          const hash = window.location.hash.substr(1);
          hash.split("&").reduce(function (_, item) {
            var parts = item.split("=");
            if (!parts[0].startsWith(historyName)) return;
            window.location.hash = window.location.hash.replace(`${item}`, "");
          }, {});
        }
      } catch (error) {
        console.error(error);
      }
    },
  },
};
</script>
<style scoped>
.has-sort {
  cursor: pointer;
}

.sort-position {
  float: right;
}

.has-sorted-th {
  background-color: #e5e7eb;
}

.has-sorted-td {
  background-color: #f9fafb;
}
</style>
