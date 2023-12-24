import { HTTP } from "../core/http-common";

const URL = "/api/dashboard";

export default {
	async getWalletInfo() {
		return HTTP.get(`${URL}/wallet-info`);
	},
	async getPositionHistogram() {
		return HTTP.get(`${URL}/position-histogram`);
	},
	async getCumulativeProfit() {
		return HTTP.get(`${URL}/cumulative-profit`);
	},
	async getImportantStats() {
		return HTTP.get(`${URL}/important-stats`);
	},
	async getPnlHistory() {
		return HTTP.get(`${URL}/pnl-history`);
	}
};
