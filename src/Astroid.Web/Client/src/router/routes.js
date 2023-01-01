import SignIn from "../pages/SignIn.vue";
import SignUp from "../pages/SignUp.vue";
import Index from "../pages/Index.vue";

//Trading Pages
import Trading from "../pages/Trading.vue";
import Dashboard from "../pages/Dashboard.vue";

import Markets from "../pages/Markets/List.vue";
import MarketSave from "../pages/Markets/Save.vue";

import Positions from "../pages/Positions.vue";

export const routes = [
	{
		name: "index",
		path: "/",
		component: Index,
	},
	{
		name: "signIn",
		path: "/sign-in",
		component: SignIn
	},
	{
		name: "signUp",
		path: "/sign-up",
		component: SignUp
	},
	{
		name: "trading",
		path: "/trading",
		component: Trading,
		children: [
			{
				name: "dashboard",
				path: "dashboard",
				component: Dashboard,
				meta: {
					title: "Dashboard",
					visible: true,
					icon: "speedometer2"
				}
			},
			{
				name: "market-list",
				path: "markets",
				component: Markets,
				meta: {
					title: "Markets",
					visible: true,
					icon: "arrow-left-right"
				},
			},
			{
				name: "market-save",
				path: "markets/save/:id?",
				component: MarketSave,
				meta: {
					title: "Save Market",
					visible: false,
					icon: "arrow-left-right"
				},
			},
			{
				name: "positions",
				path: "positions",
				component: Positions,
				meta: {
					title: "Positions",
					visible: true,
					icon: "list-nested"
				}
			},
			{
				name: "bots",
				path: "bots",
				meta: {
					title: "Bots",
					visible: true,
					icon: "gear-wide-connected"
				}
			}
		]
	}
];
