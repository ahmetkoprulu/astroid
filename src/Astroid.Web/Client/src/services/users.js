import { HTTP } from "../core/http-common";

const URL = "/api/users";

export default {
	async userInfo() {
		return HTTP.get(`${URL}/user-info`);
	},
	async getProfile() {
		return HTTP.get(`${URL}/profile`);
	},
	async saveProfile(data) {
		return HTTP.post(`${URL}/profile`, data);
	}
};
