<template>
  <b-overlay :show="busy" :opacity="1" variant="white">
    <div class="trading-container" v-if="!busy">
      <header>
        <Navbar />
        <Sidebar />
      </header>
      <main>
        <div class="container-fluid p-5" style="margin-top: 62px">
          <router-view></router-view>
        </div>
      </main>
    </div>
  </b-overlay>
</template>

<script>
import { Vue } from "../main";
import UserService from "../services/users";

import Navbar from "../components/layout/Navbar.vue";
import Sidebar from "../components/layout/Sidebar.vue";

export default {
  data() {
    return {
      busy: true,
    };
  },
  components: {
    Navbar,
    Sidebar,
  },
  async mounted() {
    var response = await UserService.userInfo();
    Vue.prototype.$user = response.data.data;

    this.busy = false;
  },
};
</script>

<style scoped>
/* .trading-container {
  width: 100vw;
  height: 100vh;
} */
</style>
