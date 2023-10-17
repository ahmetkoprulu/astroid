import { HTTP } from "../core/http-common";

const URL = "/api/positions";

export default {
	async list(filters, sorts, currentPage, perPage) {
		return HTTP.post(`${URL}/list`, { filters, sorts, currentPage, perPage });
	},
	async listOpen(filters, sorts, currentPage, perPage) {
		return HTTP.post(`${URL}/list-open`, { filters, sorts, currentPage, perPage });
	},
	async listHistory(filters, sorts, currentPage, perPage) {
		return HTTP.post(`${URL}/list-history`, { filters, sorts, currentPage, perPage });
	},
	async listOpenOrders(filters, sorts, currentPage, perPage) {
		return HTTP.post(`${URL}/list-open-orders`, { filters, sorts, currentPage, perPage });
	},
	async listTradeHistory(filters, sorts, currentPage, perPage) {
		return HTTP.post(`${URL}/list-trade-history`, { filters, sorts, currentPage, perPage });
	},
	async closePosition(id) {
		return HTTP.post(`${URL}/close/${id}`);
	}
};
