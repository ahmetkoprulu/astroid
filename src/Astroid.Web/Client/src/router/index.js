import VueRouter from "vue-router";
import { routes } from "./routes";

let router = new VueRouter({
	mode: "history",
	routes
});

export default router;
