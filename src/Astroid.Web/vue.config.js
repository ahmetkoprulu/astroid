const path = require("path");

module.exports = {
	pages: {
		index: {
			entry: "./Client/src/boot.js",
			template: "Client/public/index.html",
		},
	},
	configureWebpack: {
		resolve: {
			alias: {
				"@": path.resolve(__dirname, "./Client/src"),
			},
		},
	},
	// chainWebpack: (config) => {
	// 	config.plugin("copy").use(require("copy-webpack-plugin"), [
	// 		[
	// 			{
	// 				from: path.resolve(__dirname, "./Client/public"),
	// 				to: path.resolve(__dirname, "dist"),
	// 				toType: "dir",
	// 				ignore: [".DS_Store"],
	// 			},
	// 		],
	// 	]);
	// },
	outputDir: "./wwwroot/dist",
};