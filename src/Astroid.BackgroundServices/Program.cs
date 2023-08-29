using Astroid.BackgroundServices.Cache;
using Astroid.Core.Cache;
using Astroid.Entity;
using Astroid.Providers;

var builder = Host.CreateDefaultBuilder(args)
	.ConfigureServices((hostContext, services) =>
	{
		services.AddDbContext<AstroidDb>();
		services.AddSingleton<ICacheService, RedisCache>();
		services.AddSingleton<ExchangeInfoStore>();
		services.AddHostedService<BinanceCache>();
		services.AddHostedService<BinanceTestCache>();

	})
	.ConfigureLogging(logging =>
	{

		logging.ClearProviders();
		logging.AddConsole();
	});

var app = builder.Build();
app.Run();
