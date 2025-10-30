using System.Text.Json;
using Dapper;
using FluentValidation;
using Oms.Config;
using Oms.Services;
using WebApplication1.Dal;
using WebApplication1.DAL;
using WebApplication1.DAL.Interfaces;
using WebApplication1.DAL.Repositories;
using WebApplication1.Validators;
using OrderService = WebApplication1.BLL.Services.OrderService;
//builder
var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

DefaultTypeMap.MatchNamesWithUnderscores = true;

builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection(nameof(RabbitMqSettings)));

builder.Services.AddScoped<UnitOfWork>();

builder.Services.Configure<DbSettings>(builder.Configuration.GetSection(nameof(DbSettings)));

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderItemRepository, OrderItemRepository>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddValidatorsFromAssemblyContaining(typeof(Program));
builder.Services.AddScoped<ValidatorFactory>();
builder.Services.AddScoped<RabbitMqService>();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
});

builder.Services.AddSwaggerGen();

builder.Services.Configure<DbSettings>(builder.Configuration.GetSection(nameof(DbSettings)));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

Migrations.Program.Main([]);

app.Run();