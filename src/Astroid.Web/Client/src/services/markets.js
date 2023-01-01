import { HTTP } from "../core/http-common";

const URL = "/api/exchanges";

export default {
	async list(filters, sorts, currentPage, perPage) {
		return HTTP.post(`${URL}/list`, { filters, sorts, currentPage, perPage });
	},
	async get(id) {
		return HTTP.get(`${URL}/${id}`);
	},
	async save(model) {
		return HTTP.post(`${URL}/save`, model);
	},
	async delete(id) {
		return HTTP.delete(`${URL}/${id}`);
	},
	async getProviders() {
		return HTTP.get(`${URL}/providers`);
	}
};
