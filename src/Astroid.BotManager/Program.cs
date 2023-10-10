using Astroid.BotManager;
using Astroid.Core.Cache;
using Astroid.Core.MessageQueue;
using Astroid.Entity;
using Astroid.Providers;

var builder = Host.CreateDefaultBuilder(args)
	.ConfigureServices((hostContext, services) =>
	{
		services.AddDbContext<AstroidDb>(ServiceLifetime.Scoped);
		services.AddScoped<BinanceUsdFuturesProvider>();

		services.AddSingleton<ICacheService, RedisCache>();
		services.AddSingleton<ExchangeInfoStore>();

		services.AddSingleton<IMessageQueue, RabbitMessageQueue>();
		services.AddSingleton<BotRegistrationManager>();

		services.AddHostedService<Worker>();
	})
	.ConfigureLogging(logging =>
	{
		logging.ClearProviders();
		logging.AddConsole();
	}).ConfigureHostConfiguration(config =>
	{
		config.AddJsonFile("config.json", optional: true, reloadOnChange: true);
	});

builder.UseDefaultServiceProvider(options => options.ValidateScopes = false);

var app = builder.Build();
app.Run();
