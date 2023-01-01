<template>
  <nav
    id="main-navbar"
    class="navbar navbar-expand-lg navbar-light bg-white fixed-top shadow"
  >
    <div class="container-fluid">
      <button
        class="navbar-toggler mr-2"
        type="button"
        data-mdb-toggle="collapse"
        data-mdb-target="#sidebarMenu"
        aria-controls="sidebarMenu"
        aria-expanded="false"
        aria-label="Toggle navigation"
      >
        <b-icon icon="list" variant="dark" />
      </button>

      <a class="navbar-brand d-flex text-violet" href="/">
        <img src="../../assets/logo.png" width="35" height="35" alt="" />
        <span class="mt-1">Astroid</span>
      </a>

      <b-navbar-nav class="ml-auto">
        <b-nav-item-dropdown right>
          <!-- Using 'button-content' slot -->
          <template #button-content>
            <b-img
              v-bind="{
                src: 'https://upload.wikimedia.org/wikipedia/commons/8/89/Portrait_Placeholder.png',
                width: 25,
                height: 25,
                class: 'mr-1',
              }"
              rounded="circle"
            />

            {{ $user.name }}
          </template>
          <b-dropdown-item href="#">Profile</b-dropdown-item>
          <b-dropdown-item href="#">Sign Out</b-dropdown-item>
        </b-nav-item-dropdown>
      </b-navbar-nav>
    </div>
  </nav>
</template>

<script>
export default {
  name: "Navbar",
  data() {
    return {};
  },
  components: {},
  async mounted() {},
  methods: {},
};
</script>
<style scoped>
.shadow {
  /* z-index: 1000; */
  box-shadow: 0 1px 3px rgb(0 0 0 / 10%), 0 1px 2px rgb(0 0 0 / 6%);
}
</style>
