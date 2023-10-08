<template>
	<nav class="sidebar" :class="{ close: isClose }">
		<header class="mt-4">
			<i
				class="fa-solid fa-chevron-right toggle"
				@click="() => (isClose = !isClose)"
			/>
		</header>
		<header class="mt-3">
			<div class="image-text">
				<span class="image">
					<img src="../../assets/logo.png" width="35" height="35" alt="" />
				</span>
				<div class="text logo-text">
					<span class="name">Astroid</span>
					<!-- <span class="profession">Trading bot</span> -->
				</div>
			</div>
		</header>
		<div class="menu-bar">
			<div class="menu">
				<!-- <li class="search-box">
					<i class="bx bx-search icon"></i>
					<input type="text" placeholder="Search..." />
				</li> -->
				<ul class="menu-links">
					<li class="sidebar-link" v-for="route in routes" :key="route.name">
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
								<span class="text nav-text">
									{{ (route.meta && route.meta.title) || route.name }}
								</span>
							</a>
						</RouterLink>
					</li>
				</ul>
			</div>

			<!-- <div class="mx-4 mt-auto mb-5 d-flex">
				<div class="w-100 profile-widget">
					<div>
						<router-link
							class="link"
							:to="{ name: 'profile', params: { id: $user.id } }"
						>
							{{ $user.name }}
						</router-link>
					</div>
					<small class="text-muted">{{ $user.email | truncate(28) }}</small>
				</div>
				<i
					class="fa-solid fa-right-from-bracket fa-flip-horizontal fa-fw text-center align-self-center sign-out-icon"
					@click="signOut"
				/>
			</div> -->

			<div class="bottom-content">
				<li class="">
					<a @click="signOut">
						<i
							class="fa-solid fa-right-from-bracket fa-flip-horizontal fa-fw icon"
						/>
						<span class="text nav-text">Logout</span>
					</a>
				</li>
				<li class="mode">
					<i class="fa-solid fa-moon icon"></i>
					<span class="text nav-text">Dark</span>
				</li>
			</div>
		</div>
	</nav>
</template>
<script>
import UserService from "@/services/home.js";
export default {
	data() {
		return {
			isClose: false,
		};
	},
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
	},
};
</script>
<style>
/* Google Font Import - Poppins */
/* @import url("https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;500;600;700&display=swap"); */
* {
	margin: 0;
	padding: 0;
	box-sizing: border-box;
	font-family: "Poppins", sans-serif;
}

:root {
	/* ===== Colors ===== */
	--body-color: #e4e9f7;
	--sidebar-color: #f9fafb;
	--primary-color: #1e3a8a;
	--primary-color-light: #f6f5ff;
	--toggle-color: #d1d5db;
	--text-color: #707070;

	/* ====== Transition ====== */
	--tran-03: all 0.2s ease;
	--tran-03: all 0.3s ease;
	--tran-04: all 0.3s ease;
	--tran-05: all 0.3s ease;
}

body {
	min-height: 100vh;
	background-color: var(--body-color);
	transition: var(--tran-05);
}

::selection {
	background-color: var(--primary-color);
	color: #fff;
}

body.dark {
	--body-color: #18191a;
	--sidebar-color: #242526;
	--primary-color: #3a3b3c;
	--primary-color-light: #dbeafe;
	--toggle-color: #fff;
	--text-color: #ccc;
}

/* ===== Sidebar ===== */
.sidebar {
	height: 100%;
	width: 200px;
	padding: 10px 14px;
	background: var(--sidebar-color);
	transition: var(--tran-05);
}

.sidebar.close {
	width: 88px;
}

/* ===== Reusable code - Here ===== */
.sidebar li {
	height: 40px;
	list-style: none;
	display: flex;
	align-items: center;
	margin-top: 10px;
}

.sidebar li .router-active {
	background-color: #6b728022 !important;
}

.sidebar header .image,
.sidebar .icon {
	min-width: 60px;
	border-radius: 6px;
}

.sidebar .icon {
	min-width: 60px;
	border-radius: 6px;
	height: 100%;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 16px;
}

.sidebar .text,
.sidebar .icon {
	color: var(--text-color);
	transition: var(--tran-03);
}

.sidebar .text {
	font-size: 14px;
	font-weight: 500;
	white-space: nowrap;
	opacity: 1;
}
.sidebar.close .text {
	opacity: 0;
}
/* =========================== */

.sidebar header {
	position: relative;
}

.sidebar header .image-text {
	display: flex;
	align-items: center;
}
.sidebar header .logo-text {
	display: flex;
	flex-direction: column;
}
header .image-text .name {
	margin-top: 2px;
	font-size: 18px;
	font-weight: 600;
}

header .image-text .profession {
	font-size: 16px;
	margin-top: -2px;
	display: block;
}

.sidebar header .image {
	display: flex;
	align-items: center;
	justify-content: center;
}

.sidebar header .image img {
	width: 40px;
	border-radius: 6px;
}

.sidebar .toggle {
	position: absolute;
	top: 50%;
	right: -25px;
	transform: translateY(-50%) rotate(180deg);
	height: 25px;
	width: 25px;
	background-color: #d1d5db;
	color: var(--sidebar-color);
	border-radius: 50%;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 12px;
	cursor: pointer;
	transition: var(--tran-05);
}

body.dark .sidebar header .toggle {
	color: var(--text-color);
}

.sidebar.close .toggle {
	transform: translateY(-50%) rotate(0deg);
}

.sidebar .menu {
	margin-top: 40px;
}

.sidebar li.search-box {
	border-radius: 6px;
	background-color: var(--primary-color-light);
	cursor: pointer;
	transition: var(--tran-05);
}

.sidebar li.search-box input {
	height: 100%;
	width: 100%;
	outline: none;
	border: none;
	background-color: var(--primary-color-light);
	color: var(--text-color);
	border-radius: 6px;
	font-size: 17px;
	font-weight: 500;
	transition: var(--tran-05);
}
.sidebar li a {
	list-style: none;
	height: 100%;
	background-color: transparent;
	display: flex;
	align-items: center;
	height: 100%;
	width: 100%;
	border-radius: 6px;
	text-decoration: none;
	transition: var(--tran-03);
}

.sidebar li a:hover {
	background-color: #f3f4f6;
}
.sidebar li a:hover .icon,
.sidebar li a:hover .text {
	color: var(#000);
}
body.dark .sidebar li a:hover .icon,
body.dark .sidebar li a:hover .text {
	color: var(--text-color);
}

.sidebar .menu-bar {
	height: calc(100% - 55px);
	display: flex;
	flex-direction: column;
	justify-content: space-between;
	overflow-y: scroll;
}
.menu-bar::-webkit-scrollbar {
	display: none;
}
.sidebar .menu-bar .mode {
	border-radius: 6px;
	/* background-color: var(--primary-color-light); */
	position: relative;
	transition: var(--tran-05);
}

.menu-bar .mode .sun-moon {
	height: 50px;
	width: 60px;
}

.mode .sun-moon i {
	position: absolute;
}
.mode .sun-moon i.sun {
	opacity: 0;
}
body.dark .mode .sun-moon i.sun {
	opacity: 1;
}
body.dark .mode .sun-moon i.moon {
	opacity: 0;
}

.menu-bar .bottom-content .toggle-switch {
	position: absolute;
	right: 0;
	height: 90%;
	min-width: 60px;
	display: flex;
	align-items: center;
	justify-content: center;
	border-radius: 6px;
	cursor: pointer;
}
.toggle-switch .switch {
	position: relative;
	height: 22px;
	width: 40px;
	border-radius: 25px;
	background-color: var(--toggle-color);
	transition: var(--tran-05);
}

.switch::before {
	content: "";
	position: absolute;
	height: 15px;
	width: 15px;
	border-radius: 50%;
	top: 50%;
	left: 5px;
	transform: translateY(-50%);
	background-color: var(--sidebar-color);
	transition: var(--tran-04);
}

body.dark .switch::before {
	left: 20px;
}

.home {
	position: absolute;
	top: 0;
	top: 0;
	left: 250px;
	height: 100vh;
	width: calc(100% - 250px);
	background-color: var(--body-color);
	transition: var(--tran-05);
}
.home .text {
	font-size: 30px;
	font-weight: 500;
	color: var(--text-color);
	padding: 12px 60px;
}

.sidebar.close ~ .home {
	left: 78px;
	height: 100vh;
	width: calc(100% - 78px);
}
body.dark .home .text {
	color: var(--text-color);
}
</style>
