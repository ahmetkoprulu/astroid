import App from "./pages/App.vue";

import { Vue, router } from "./main.js";

new Vue({
	router,
	render: h => h(App),
}).$mount('#app')
