using Astroid.BackgroundServices.Cache;
using Astroid.Core.Cache;
using Astroid.Core.MessageQueue;
using Astroid.Entity;
using Astroid.Providers;

var builder = Host.CreateDefaultBuilder(args)
	.ConfigureServices((hostContext, services) =>
	{
		services.AddDbContext<AstroidDb>();

		services.AddSingleton<ICacheService, RedisCache>();
		services.AddSingleton<ExchangeInfoStore>();

		services.AddSingleton<IMessageQueue, RabbitMessageQueue>();
		services.AddSingleton<AQOrder>();

		services.AddHostedService<BinanceCache>();
		services.AddHostedService<BinanceTestCache>();
		services.AddHostedService<StopLossWatcher>();
		services.AddHostedService<TakeProfitWatcher>();
	})
	.ConfigureLogging(logging =>
	{
		logging.ClearProviders();
		logging.AddConsole();
	}).ConfigureHostConfiguration(config =>
	{
		config.AddJsonFile("config.json", optional: true, reloadOnChange: true);
	});

var app = builder.Build();
app.Run();
