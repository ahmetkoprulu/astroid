<template>
	<b-overlay :show="loading" :opacity="1" variant="white">
		<div class="trading-container d-flex" v-if="!loading">
			<!-- <Navbar /> -->
			<Sidebar />
			<main class="main-container w-100">
				<b-overlay
					class="mx-lg-5 mx-md-3 mx-sm-0"
					style="height: 100vh"
					:show="$busy"
					no-fade
					:opacity="1"
					variant="white"
				>
					<div class="pb-5">
						<router-view></router-view>
					</div>
				</b-overlay>
			</main>
		</div>
	</b-overlay>
</template>

<script>
import UserService from "../services/users";

// import Navbar from "../components/layout/Navbar.vue";
import Sidebar from "../components/layout/Sidebar.vue";

export default {
	data() {
		return {
			loading: true,
		};
	},
	components: {
		// Navbar,
		Sidebar,
	},
	async mounted() {
		var response = await UserService.getProfile();
		this.$user = response.data.data;

		this.loading = false;
	},
};
</script>

<style scoped>
/* .trading-container {
  width: 100vw;
  height: 100vh;
} */
</style>
