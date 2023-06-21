<template>
	<b-overlay style="height: 100vh" :show="loading" :opacity="1" variant="white">
		<div class="trading-container" v-if="!loading">
			<header>
				<!-- <Navbar /> -->
				<Sidebar />
			</header>
			<main>
				<b-overlay
					style="height: 100vh"
					:show="$busy"
					no-fade
					:opacity="1"
					variant="white"
				>
					<div class="container-fluid">
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
