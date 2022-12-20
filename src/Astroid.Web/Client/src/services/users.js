import { HTTP } from "../core/http-common";

const URL = "/api/users";

export default {
	async userInfo() {
		return HTTP.get(`${URL}/user-info`);
	}
};
