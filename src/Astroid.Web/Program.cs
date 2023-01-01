using System.Net;
using System.Security.Cryptography.X509Certificates;
using Astroid.Core;
using Astroid.Entity;
using Astroid.Web;
using Astroid.Web.Middleware;
using Microsoft.AspNetCore.SpaServices;
using VueCliMiddleware;

var builder = WebApplication.CreateBuilder(args);

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
	return null;
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

// Dependency Injections
builder.Services.AddDbContext<AstroidDb>();

// Add services to the container.
builder.Services.AddControllers();

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

var app = builder.Build();
var conf = app.Configuration.Get<AConfAppSettings>();
// using var db = app.Services.GetRequiredService<AstroidDb>();
// db.Database.EnsureCreated();

if (app.Environment.IsDevelopment())
{
	app.UseDeveloperExceptionPage();
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseSpaStaticFiles();

// app.UseHttpsRedirection();

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
app.UseCors();

await app.SeedDatabase();

app.Run();