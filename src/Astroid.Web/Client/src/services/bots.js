import { HTTP } from "../core/http-common";

const URL = "/api/bots";

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
	async enable(id) {
		return HTTP.patch(`${URL}/${id}/enable`);
	},
	async changeMarginType(id, marginType, tickers) {
		return HTTP.put(`${URL}/${id}/margin-type?type=${marginType}&tickers=${tickers}`);
	},
	async testComputationMethod(ticker, bot) {
		return HTTP.post(`${URL}/${ticker}/test-computation-method`, bot);
	}
};
