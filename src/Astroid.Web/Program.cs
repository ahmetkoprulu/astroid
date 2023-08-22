using System.Net;
using System.Security.Cryptography.X509Certificates;
using Astroid.Core;
using Astroid.Core.Cache;
using Astroid.Entity;
using Astroid.Web;
using Astroid.Web.Middleware;
using Microsoft.AspNetCore.SpaServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VueCliMiddleware;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Astroid.Web.Helpers;
using Astroid.Web.Cache;
using Astroid.Providers;

var builder = WebApplication.CreateBuilder(args);

var environmantName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
builder.Environment.EnvironmentName = environmantName ?? "Development";

builder.WebHost.ConfigureKestrel((context, options) =>
{
	var remoteConfig = context.Configuration.Get<AConfAppSettings>();
	if (remoteConfig?.Endpoints == null || !remoteConfig.Endpoints.Any()) return;
	foreach (var endpoint in remoteConfig.Endpoints)
	{
		var url = new Uri(endpoint.Url!);

		var port = url.Port != 0 ? url.Port : url.Scheme == "https" ? 443 : 80;

		var ipAddresses = new List<IPAddress>();
		if (url.Host == "localhost")
		{
			ipAddresses.Add(IPAddress.IPv6Loopback);
			ipAddresses.Add(IPAddress.Loopback);
			ipAddresses.Add(IPAddress.Any);
		}
		else if (IPAddress.TryParse(url.Host, out var address))
			ipAddresses.Add(address);
		else
			ipAddresses.Add(IPAddress.IPv6Any);

		foreach (var address in ipAddresses)
		{
			options.Listen(address, port, listenOptions =>
			{
				if (url.Scheme != "https") return;
				var certificate = LoadCertificate(endpoint, url);
				if (certificate != null)
					listenOptions.UseHttps(certificate);
			});
		}
	}
});

static X509Certificate2 LoadCertificate(AConfEndpoint config, Uri url)
{
	if (config.Certificate == null) return null;
	if (config.Certificate.StoreName != null && config.Certificate.StoreLocation != null)
	{
		var storeLocation = Enum.Parse<StoreLocation>(config.Certificate.StoreLocation);
		using var store = new X509Store(config.Certificate.StoreName, storeLocation);
		store.Open(OpenFlags.ReadOnly);
		var certificate = store.Certificates.Find(X509FindType.FindBySubjectName, url.Host, config.Certificate.Validate);
		if (certificate.Count == 0) certificate = store.Certificates.Find(X509FindType.FindByThumbprint, config.Certificate.Thumbprint, config.Certificate.Validate);

		if (certificate.Count == 0) throw new InvalidOperationException($"Certificate not found for {url.Host}.");

		return certificate[0];
	}

	if (config.Certificate.File != null && config.Certificate.Password != null)
		return new X509Certificate2(config.Certificate.File, config.Certificate.Password);

	throw new InvalidOperationException("No valid certificate configuration found for the current endpoint.");
}

// Configurations
builder.Configuration.SetBasePath(builder.Environment.ContentRootPath);
if (builder.Environment.IsProduction())
	builder.Configuration.AddJsonFile("config.prod.json", optional: true, reloadOnChange: true);
else
	builder.Configuration.AddJsonFile("config.dev.json", optional: true, reloadOnChange: true);

// Dependency Injections
builder.Services.AddDbContext<AstroidDb>();
builder.Services.AddSingleton<ICacheService, RedisCache>();
builder.Services.AddSingleton<ExchangeInfoStore>();

builder.Services.AddSingleton<BinanceCacheFeed>();
builder.Services.AddSingleton<BinanceTestCacheFeed>();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
	options.SuppressModelStateInvalidFilter = true;
});

// Add services to the container.
builder.Services.AddControllers()
	.AddNewtonsoftJson(options =>
	{
		options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
		options.SerializerSettings.ContractResolver = new DefaultContractResolver
		{
			NamingStrategy = new CamelCaseNamingStrategy()
		};
	});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Authentication things
builder.Services
	.AddAuthentication(ACWeb.Authentication.DefaultSchema)
	.AddCookie(ACWeb.Authentication.DefaultSchema, options =>
		{
			options.Cookie.Name = ACWeb.Authentication.DefaultSchema;
			options.Cookie.Path = "/";
			options.Cookie.SameSite = SameSiteMode.Strict;
			options.LoginPath = new PathString(ACWeb.Authentication.SignInPath);
			options.LogoutPath = new PathString(ACWeb.Authentication.SignOutPath);
			options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
		}
	);
builder.Services.AddAuthorization();

// In production, the Vue files will be served from this directory
builder.Services.AddSpaStaticFiles(configuration => configuration.RootPath = "wwwroot/dist");

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
if (builder.Environment.IsProduction())
	builder.Logging.AddFile("Logs/astroid-{Date}.log", retainedFileCountLimit: 5);

var app = builder.Build();
var conf = app.Configuration.Get<AConfAppSettings>();

// Ensure the database is created and seeded.
using var scope = app.Services.CreateScope();
using var db = scope.ServiceProvider.GetRequiredService<AstroidDb>();
db.Database.Migrate();

await app.SeedDatabase();

// Cache exchange info
using var binanceFeed = scope.ServiceProvider.GetRequiredService<BinanceCacheFeed>();
using var binanceTestFeed = scope.ServiceProvider.GetRequiredService<BinanceTestCacheFeed>();

await binanceFeed.StartSubscriptions();
await binanceTestFeed.StartSubscriptions();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseDeveloperExceptionPage();
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
if (app.Environment.IsProduction())
{
	app.UseHsts();
	app.UseSpaStaticFiles();
}

app.UseMiddleware<ErrorHandlerMiddleware>();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseEndpoints(_ => { });

app.UseEndpoints(e =>
{
	e.MapControllers();
	if (app.Environment.IsDevelopment())
	{
		e.MapToVueCliProxy(
			"{*path}",
			new SpaOptions { SourcePath = "./" },
			npmScript: "serve",
			regex: "Compiled successfully",
			forceKill: true
		);
	}
});

app.UseSpa(spa =>
{
	spa.Options.SourcePath = "wwwroot/dist";
});

app.UseCors();

app.Run();
