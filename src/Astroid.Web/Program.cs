using Astroid.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// In production, the Vue files will be served from this directory
builder.Services.AddSpaStaticFiles(configuration => configuration.RootPath = "wwwroot/dist");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseSpaStaticFiles();

app.UseHttpsRedirection();

app.UseMiddleware<ErrorHandlerMiddleware>();

app.UseAuthorization();

app.UseRouting();

app.MapControllers();

app.UseCors();

app.Run();

app.UseSpa(spa =>
{
	spa.Options.SourcePath = "Client";
	if (app.Environment.IsDevelopment())
	{
		spa.UseProxyToSpaDevelopmentServer("http://localhost:4156");
	}
	// 	spa.UseVueDevServer(npmScript: "start", matchText: "Compiled successfully", useHttps: true);
});
