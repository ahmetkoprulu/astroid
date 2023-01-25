import { HTTP } from "../core/http-common";

const URL = "/api/audits";

export default {
	async list(botId, filters, sorts, currentPage, perPage) {
		return HTTP.post(`${URL}/list?bot=${botId}`, { filters, sorts, currentPage, perPage });
	}
};
