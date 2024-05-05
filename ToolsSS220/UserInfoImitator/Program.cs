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

    service.AddSwaggerGen();
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseEndpoints(config =>
{
    config.MapControllers();
});

app.Run();
