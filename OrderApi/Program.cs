using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderApi.Data;
using OrderApi.Infrastructure;
using OrderApi.Models;
using SharedModels;
using Order = OrderApi.Models.Order;

var builder = WebApplication.CreateBuilder(args);

// Base URL for the product service when the solution is executed using docker-compose.
// The product service (running as a container) listens on this URL for HTTP requests
// from other services specified in the docker compose file (which in this solution is
// the order service).
string productServiceBaseUrl = "http://productapi/products/";

// Add services to the container.

string cloudAMQPConnectionString =
    "host=rabbitmq";

builder.Services.AddDbContext<OrderApiContext>(opt => opt.UseInMemoryDatabase("OrdersDb"));
// Register repositories for dependency injection
builder.Services.AddScoped<IOrderRepository<Order>, OrderOrderRepository>();
builder.Services.AddSingleton<IOrderConverter, OrderConverter>();
// Register database initializer for dependency injection
builder.Services.AddTransient<IDbInitializer, DbInitializer>();
builder.Services.AddSingleton<IMessagePublisher>(new MessagePublisher(cloudAMQPConnectionString));
// Register product service gateway for dependency injection
builder.Services.AddSingleton<IServiceGateway<ProductDto>>(new ProductServiceGateway(productServiceBaseUrl));
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Initialize the database.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetService<OrderApiContext>();
    var dbInitializer = services.GetService<IDbInitializer>();
    dbInitializer.Initialize(dbContext);
}

Task.Factory.StartNew(() => new MessageListener(app.Services, cloudAMQPConnectionString).Start());

app.UseAuthorization();

app.MapControllers();

app.Run();
