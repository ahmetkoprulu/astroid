using Astroid.BackgroundServices.Cache;
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
		services.AddSingleton<AQOrder>();

		services.AddHostedService<BinanceCache>();
		services.AddHostedService<BinanceTestCache>();
		services.AddHostedService<OrderWatcher>();
	})
	.ConfigureLogging(logging =>
	{
		logging.ClearProviders();
		logging.AddConsole();
	}).ConfigureHostConfiguration(config =>
	{
		config.AddJsonFile("config.json", optional: true, reloadOnChange: true);
	});

ThreadPool.GetMinThreads(out var workerThreads, out var completionPortThreads);
Console.WriteLine($"Min worker threads: {workerThreads}, Min completion port threads: {completionPortThreads}");

var success = ThreadPool.SetMinThreads(32, 32);
if (success)
{
	ThreadPool.GetMinThreads(out var newWorkerThreads, out var newCompletionPortThreads);
	Console.WriteLine($"Min worker threads: {newWorkerThreads}, Min completion port threads: {newCompletionPortThreads}");
}

var app = builder.Build();
app.Run();
