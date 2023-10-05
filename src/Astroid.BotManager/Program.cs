using Astroid.BackgroundServices.Order;
using Astroid.Core.Cache;
using Astroid.Core.MessageQueue;
using Astroid.Entity;
using Astroid.Providers;

var builder = Host.CreateDefaultBuilder(args)
	.ConfigureServices((hostContext, services) =>
	{
		services.AddDbContext<AstroidDb>(ServiceLifetime.Transient);

		services.AddSingleton<ICacheService, RedisCache>();
		services.AddSingleton<ExchangeInfoStore>();

		services.AddSingleton<IMessageQueue, RabbitMessageQueue>();

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

var app = builder.Build();
app.Run();
