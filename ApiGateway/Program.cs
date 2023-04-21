using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

//app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();
app.UseHttpMetrics();

app.UseEndpoints(endpoints =>
{
    endpoints.MapMetrics();
    endpoints.MapGet("/", async context =>
    {
        await context.Response.WriteAsync("Hello world!");
    });
});

app.MapControllers();

app.UseOcelot().Wait();

app.Run();