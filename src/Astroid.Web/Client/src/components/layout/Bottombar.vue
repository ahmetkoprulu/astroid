<template>
	<div class="bottombar justify-content-between px-md-5 px-3">
		<div id="logo-section" class="my-auto">
			<!-- <svg
				xmlns="http://www.w3.org/2000/svg"
				viewBox="0 0 26 26"
				enable-background="new 0 0 26 26"
				width="35"
				height="35"
			>
				<path
					style="
						text-indent: 0;
						text-align: start;
						line-height: normal;
						text-transform: none;
						block-progression: tb;
						-inkscape-font-specification: Bitstream Vera Sans;
					"
					d="M 22 0 C 21.447715 0 21 0.44771525 21 1 C 21 1.5522847 21.447715 2 22 2 C 22.552285 2 23 1.5522847 23 1 C 23 0.44771525 22.552285 0 22 0 z M 14 1 C 13.447715 1 13 1.4477153 13 2 C 13 2.5522847 13.447715 3 14 3 C 14.552285 3 15 2.5522847 15 2 C 15 1.4477153 14.552285 1 14 1 z M 20 2 C 19.447715 2 19 2.4477153 19 3 C 19 3.5522847 19.447715 4 20 4 C 20.552285 4 21 3.5522847 21 3 C 21 2.4477153 20.552285 2 20 2 z M 11.96875 3.09375 A 1.0001 1.0001 0 0 0 11.40625 3.40625 L 2.40625 12.40625 C -0.6865871 15.499087 -0.6865871 20.625913 2.40625 23.71875 C 5.4990871 26.811587 10.625913 26.811587 13.71875 23.71875 L 20.71875 16.71875 A 1.016466 1.016466 0 1 0 19.28125 15.28125 L 12.28125 22.28125 C 9.9740871 24.588413 6.1196629 24.588413 3.8125 22.28125 C 1.5053371 19.974087 1.5053371 16.119663 3.8125 13.8125 L 12.8125 4.8125 A 1.0001 1.0001 0 0 0 11.96875 3.09375 z M 18 4 C 17.447715 4 17 4.4477153 17 5 C 17 5.5522847 17.447715 6 18 6 C 18.552285 6 19 5.5522847 19 5 C 19 4.4477153 18.552285 4 18 4 z M 25 4 C 24.447715 4 24 4.4477153 24 5 C 24 5.5522847 24.447715 6 25 6 C 25.552285 6 26 5.5522847 26 5 C 26 4.4477153 25.552285 4 25 4 z M 15.875 6 A 1.0001 1.0001 0 0 0 15.28125 6.28125 L 13.28125 8.28125 A 1.016466 1.016466 0 1 0 14.71875 9.71875 L 16.71875 7.71875 A 1.0001 1.0001 0 0 0 15.875 6 z M 22.875 6 A 1.0001 1.0001 0 0 0 22.28125 6.28125 L 15.28125 13.28125 A 1.016466 1.016466 0 1 0 16.71875 14.71875 L 23.71875 7.71875 A 1.0001 1.0001 0 0 0 22.875 6 z M 24 11 C 23.447715 11 23 11.447715 23 12 C 23 12.552285 23.447715 13 24 13 C 24.552285 13 25 12.552285 25 12 C 25 11.447715 24.552285 11 24 11 z M 9.90625 12.90625 C 8.0798096 12.931494 6.1375 14.2 5.3125 15.09375 C 3.7125 16.69375 3.7125 19.2125 5.3125 20.8125 C 5.5125 20.7125 5.70625 20.6875 5.90625 20.6875 C 6.60625 20.6875 7.0875 21.20625 7.1875 21.90625 C 8.4875 22.20625 10 21.90625 11 20.90625 C 11.5 20.40625 12.00625 19.7 12.40625 19 C 12.20625 18.7 12.09375 18.30625 12.09375 17.90625 C 11.89375 17.00625 12.39375 16.30625 13.09375 15.90625 C 12.99375 15.20625 12.7875 14.5125 12.1875 13.8125 C 11.53125 13.15625 10.73645 12.894775 9.90625 12.90625 z M 22 13 C 21.447715 13 21 13.447715 21 14 C 21 14.552285 21.447715 15 22 15 C 22.552285 15 23 14.552285 23 14 C 23 13.447715 22.552285 13 22 13 z M 7.53125 15.5 C 8.065625 15.51875 8.5875 15.88125 8.8125 16.40625 C 9.0125 17.10625 8.7125 17.7 8.3125 18 C 8.3125 17.9 8.2875 17.79375 8.1875 17.59375 C 7.8875 16.89375 7.10625 16.5125 6.40625 16.8125 C 6.30625 16.8125 6.19375 16.9 6.09375 17 C 6.09375 16.4 6.4 15.79375 7 15.59375 C 7.175 15.51875 7.353125 15.49375 7.53125 15.5 z"
				/>
			</svg> -->
		</div>
		<div id="menu-section" class="menu-bar">
			<div class="menu-item" v-for="route in routes" :key="route.name">
				<RouterLink
					:to="resolveRoute(route).fullPath"
					v-slot="{ href, isActive, isExactActive }"
					custom
				>
					<a
						@click="(e) => onButtonClick(e, route, href)"
						class="nav-item-row"
						:class="[
							isActive && 'router-active',
							isExactActive && 'router-exact',
						]"
					>
						<i :class="`icon fa-fw ${route.meta.icon}`" />
					</a>
				</RouterLink>
			</div>
		</div>
		<div id="right"></div>
	</div>
</template>
<script>
import UserService from "@/services/home.js";

export default {
	computed: {
		routes() {
			const tradeRoutes = this.$router.options.routes.find(
				(x) => x.name == "trading"
			);

			if (!tradeRoutes) return [];

			return tradeRoutes.children.filter((route) => {
				const isVisible = route.meta && route.meta.visible;
				if (!isVisible) return false;

				if (!route.children || route.children.length <= 0) return true;

				const children = route.children.filter((x) => x.meta && x.meta.visible);

				if (children.length <= 0) return isVisible;
			});
		},
	},
	methods: {
		resolveRoute(route) {
			const resolvedRoute = this.$router.resolve({
				name: route.name,
			});
			return resolvedRoute.route;
		},
		async signOut() {
			var response = await UserService.signOut();
			if (response.data.success) this.$router.push({ name: "signIn" });
		},
		onButtonClick(e, route, href) {
			e.preventDefault();
			if (window.location.pathname == href) return;

			this.$router.push(route);
		},
		toggleTheme() {
			this.$theme = this.$theme == "dark" ? "light" : "dark";
			this.theme = this.$theme;
		},
	},
};
</script>
<style>
.bottombar {
	background-color: var(--md-sys-color-background);
	color: var(--md-sys-color-on-background) !important;
	fill: var(--md-sys-color-on-background);
	overflow: hidden;
	position: fixed;
	bottom: 0;
	width: 100%;
	z-index: 99;
	display: none;
	border-top: 1px solid var(--md-sys-color-surface-9);
}

.bottombar .menu-bar .menu-item a.router-active {
	background-color: var(--md-sys-color-primary) !important;
	color: var(--md-sys-color-on-primary) !important;
}

/* .bottombar .menu-bar .menu-item:hover {
	background-color: var(--md-sys-color-secondary-container);
	color: var(--md-sys-color-on-secondary-container);
} */

.menu-bar::-webkit-scrollbar {
	display: none;
}

.bottombar .menu-bar {
	padding: 0 40px;
	display: flex;
	justify-content: space-between;
	height: 60px;
	width: 100%;
	font-size: 18px;
}

.bottombar .menu-bar .menu-item {
	height: 100%;
	width: 100%;
	padding: 10px;
	display: flex;
	flex-wrap: wrap;
	justify-content: center;
	align-content: center;
	color: var(--md-sys-color-on-background) !important;
}

.bottombar .menu-bar .menu-item a {
	display: flex;
	width: 100%;
	height: 100%;
	border-radius: 10px;
	align-content: center;
	justify-content: center;
	flex-wrap: wrap;
	text-decoration: none;
	color: var(--md-sys-color-on-background) !important;
}
</style>
