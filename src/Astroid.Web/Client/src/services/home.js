import { HTTP } from "../core/http-common";

const URL = "/api";

export default {
	async signUp(data) {
		return HTTP.post(`${URL}/sign-up`, data);
	},
	async signIn(data) {
		return HTTP.post(`${URL}/sign-in`, data);
	},
	async signOut() {
		return HTTP.get(`${URL}/sign-out`);
	},
	async getSymbolsByExchangeName(name) {
		return HTTP.get(`${URL}/exchange/${name}/symbols`);
	}
};
