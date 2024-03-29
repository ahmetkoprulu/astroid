import SignIn from "../pages/SignIn.vue";
import SignUp from "../pages/SignUp.vue";
import Index from "../pages/Index.vue";

//Trading Pages
import Trading from "../pages/Trading.vue";
import Dashboard from "../pages/Dashboard.vue";

import Markets from "../pages/Markets/List.vue";
import MarketSave from "../pages/Markets/Save.vue";

import Bots from "../pages/Bots/List.vue";
import BotSave from "../pages/Bots/Save.vue";
import BotAudits from "../pages/Bots/Audits.vue";

import Terminal from "../pages/Terminal.vue";

import Positions from "../pages/Positions.vue";

import Profile from "../pages/Profile.vue";

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
					icon: "fa-solid fa-chart-pie"
				}
			},
			{
				name: "market-list",
				path: "markets",
				component: Markets,
				meta: {
					title: "Wallet",
					visible: true,
					icon: "fa-solid fa-wallet"
				},
			},
			{
				name: "market-save",
				path: "markets/save/:id?",
				component: MarketSave,
				meta: {
					title: "Save Wallet",
					visible: false,
				},
			},
			{
				name: "bot-list",
				path: "bots",
				component: Bots,
				meta: {
					title: "Bots",
					visible: true,
					icon: "fa-solid fa-robot"
				}
			},
			{
				name: "bot-save",
				path: "bots/save/:id?",
				component: BotSave,
				meta: {
					title: "Save Bot",
					visible: false,
				}
			},
			{
				name: "bot-audits",
				path: "bots/:id/audits",
				component: BotAudits,
				meta: {
					title: "Bot Audits",
					visible: false,
				}
			},
			{
				name: "terminal",
				path: "termianl",
				component: Terminal,
				meta: {
					title: "Terminal",
					visible: true,
					icon: "fa-solid fa-chart-line"
				}
			},
			{
				name: "positions",
				path: "positions",
				component: Positions,
				meta: {
					title: "Positions",
					visible: true,
					icon: "fa-solid fa-bars-staggered"
				}
			},
			{
				name: "profile",
				path: "profile/:id",
				component: Profile,
				meta: {
					title: "Profile",
					visible: false,
				}
			}
		]
	}
];
