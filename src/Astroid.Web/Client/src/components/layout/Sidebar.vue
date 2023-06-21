<template>
	<nav id="sidebarMenu" class="collapse d-lg-block sidebar collapse">
		<div class="position-sticky h-100 d-flex flex-column">
			<a class="navbar-brand d-flex text-dark mx-4 my-4" href="/">
				<img src="../../assets/logo.png" width="35" height="35" alt="" />
				<span class="mt-1">Astroid</span>
			</a>

			<div class="mx-3">
				<div class="nav-item" v-for="route in routes" :key="route.name">
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
							<span class="nav-item-content">
								<div class="nav-item-icon-container">
									<i :class="route.meta.icon" />
								</div>
								<span>{{
									(route.meta && route.meta.title) || route.name
								}}</span>
							</span>
						</a>
					</RouterLink>
				</div>
			</div>

			<div class="mx-4 mt-auto mb-5 d-flex">
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
			</div>
		</div>
	</nav>
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
	},
};
</script>
<style>
.sidebar {
	position: fixed;
	top: 0;
	bottom: 0;
	left: 0;
	box-shadow: 0 2px 5px 0 rgb(0 0 0 / 5%), 0 2px 10px 0 rgb(0 0 0 / 5%);
	width: 240px;
	z-index: 600;
	background-color: rgba(249, 250, 251, var(--tw-bg-opacity));
}

@media (max-width: 991.98px) {
	.sidebar {
		width: 100%;
	}
}
.sidebar .active {
	border-radius: 5px;
	box-shadow: 0 2px 5px 0 rgb(0 0 0 / 16%), 0 2px 10px 0 rgb(0 0 0 / 12%);
}

.sidebar-sticky {
	position: relative;
	top: 0;
	height: calc(100vh - 48px);
	padding-top: 0.5rem;
	overflow-x: hidden;
	overflow-y: auto; /* Scrollable contents if viewport is shorter than content. */
}

.nav-item {
	margin-bottom: 4px;
}

.nav-item .nav-item .nav-item-row {
	padding-left: 4px;
}

.nav-item .nav-item .nav-item-row .nav-item-content {
	padding-left: 2px;
}

.nav-item .nav-item .nav-item-content {
	height: 32px;
}

.nav-item-row {
	user-select: none;

	position: relative;
	display: block;
	text-decoration: none !important;
	color: var(--mui-c-grey-500);
	/* button \/ */
	cursor: pointer;
	font: inherit;
	border: 0;
	background-color: transparent;
	width: 100%;
	text-align: left;
}

.nav-item-row:before {
	content: "";
	position: absolute;
	left: 0;
	top: 0;
	height: 100%;
	width: 4px;
	background-color: currentColor;
	border-radius: 0 4px 4px 0;
	opacity: 0;
}

.nav-item-row:hover .nav-item-content {
	background-color: #eee;
	color: var(--mui-c-grey-800);
}

/* OPEN */

.nav-item-row.open .nav-item-arrow {
	transform: rotate(180deg);
}

.navigation .nav-item .nav-item:first-child {
	margin-top: 10px;
}

.navigation .nav-item .nav-item:last-child {
	margin-bottom: 10px;
}

/* ACTIVE */
.nav-item-row.router-active {
	color: var(--mui-c-grey-800) !important;
}

.nav-item > .nav-item-row.router-active .nav-item-content {
	background: rgb(4 40 96 / 11%) !important;
	border-radius: 6px;
}

.nav-item .nav-item .nav-item-row.router-active.child .nav-item-content {
	background: none;
}

.nav-item-row:hover .nav-item-icon {
	color: inherit;
}

.nav-item-row.router-active .nav-item-icon {
	color: inherit;
}

/* EXACT */

.nav-item-row.router-active.child,
.nav-item-row.router-exact {
	color: var(--mui-c-violet-600);
}

.nav-item .nav-item .nav-item-row.router-active.child:before,
.nav-item .nav-item .nav-item-row.router-exact:before {
	content: "";
	position: absolute;
	left: -2px;
	top: 10px;
	height: 16px;
	width: 3px;
	background-color: currentColor;
	border-radius: 4px;
	opacity: 1;
}

.nav-item-content {
	font-size: var(--mui-text-sm);
	width: 100%;
	display: grid;
	grid-template-columns: 24px 1fr 24px;
	grid-gap: 8px;
	align-items: center;
	height: 36px;
	color: inherit;
	padding-left: 12px;
	font-weight: var(--mui-text-semibold);
	border-radius: var(--mui-radius);
}

.nav-item-icon-container {
	display: flex;
	align-items: center;
	justify-content: center;
}

.nav-item-icon {
	color: var(--mui-c-grey-500);
	resize: "horizontal";
	overflow: "hidden";
	width: "1000px";
	height: "auto";
}

.nav-item-arrow {
	color: var(--mui-c-grey-500);
	transition: 100ms;
}

.profile-widget {
	font-size: var(--mui-text-sm);
	font-weight: var(--mui-text-semibold);
	color: var(--mui-c-grey-500);
}

.sign-out-icon {
	cursor: pointer;
}

.link {
	color: var(--mui-c-grey-500);
	text-decoration: none !important;
}
</style>
