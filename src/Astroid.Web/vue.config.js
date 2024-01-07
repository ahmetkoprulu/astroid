const path = require("path");

module.exports = {
	parallel: false,
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
		module: {
			rules: [
				// {
				// 	test: /\.js$/,
				// 	loader: 'babel-loader',
				// 	options: {
				// 		presets: [
				// 			[
				// 				"@babel/preset-env",
				// 				{
				// 					targets: {
				// 						node: "current",
				// 					},
				// 					include: [
				// 						"@babel/plugin-proposal-optional-chaining",
				// 						"@babel/plugin-proposal-nullish-coalescing-operator",
				// 					]
				// 				},
				// 			],
				// 		],
				// 		plugins: [
				// 			"@babel/plugin-proposal-optional-chaining",
				// 			"@babel/plugin-proposal-nullish-coalescing-operator",
				// 		],
				// 		include: [
				// 			/Client/,
				// 		],
				// 	}
				// },
				// {
				// 	test: /\.css$/i,
				// 	use: [
				// 		"style-loader",
				// 		"css-loader",
				// 		"postcss-loader"
				// 	],
				// }
			]
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