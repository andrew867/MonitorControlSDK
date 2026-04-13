using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using Sony.MonitorControl.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JsonOptions>(o =>
{
	o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
	o.SerializerOptions.WriteIndented = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(static c =>
{
	c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
	{
		Title = "MonitorControl HTTP API",
		Version = "v1",
		Description = "SDAP discovery and SDCP/VMC/VMS/VMA control. Firmware routes require configuration + X-Firmware-Ack: CONFIRM.",
	});
});

builder.Services.AddCors(o => o.AddDefaultPolicy(static p =>
	p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseCors();
app.UseWebSockets();
app.UseSwagger();
app.UseSwaggerUI(static o => o.SwaggerEndpoint("/swagger/v1/swagger.json", "MonitorControl v1"));
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapMonitorControlApi();
app.MapMonitorPushEndpoints();
app.MapFallbackToFile("index.html");

app.Run();
