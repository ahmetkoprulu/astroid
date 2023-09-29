import { HTTP } from "../core/http-common";

const URL = "/api/positions";

export default {
	async list(filters, sorts, currentPage, perPage) {
		return HTTP.post(`${URL}/list`, { filters, sorts, currentPage, perPage });
	},
	async closePosition(id) {
		return HTTP.post(`${URL}/close/${id}`);
	}
};
