using System.Text.Json.Serialization;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Host.ConfigureServices((_, service) =>
{
	service
		.AddControllers()
		.AddJsonOptions(config =>
		{
			config.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
			config.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
		});
});

var app = builder.Build();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.Run();
