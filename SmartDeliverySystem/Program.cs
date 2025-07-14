using Microsoft.EntityFrameworkCore;
using SmartDeliverySystem.Services;
using SmartDeliverySystem.Data;
using SmartDeliverySystem.Middleware;
using Azure.Messaging.ServiceBus;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Add AutoMapper
builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddAutoMapper(typeof(SmartDeliverySystem.Mappings.MappingProfile));
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<DeliveryContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure JSON serialization to ignore cycles
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Services
builder.Services.AddScoped<IDeliveryService, DeliveryService>();
builder.Services.AddScoped<IServiceBusService, ServiceBusService>();
builder.Services.AddScoped<ISignalRService, SignalRService>();
builder.Services.AddScoped<ITableStorageService, TableStorageService>();

// SignalR
builder.Services.AddSignalR();

// Azure Service Bus
builder.Services.AddSingleton(provider =>
{
    var connectionString = builder.Configuration.GetConnectionString("ServiceBus");
    return new Azure.Messaging.ServiceBus.ServiceBusClient(connectionString);
});

// Azure Table Storage
builder.Services.AddSingleton(provider =>
{
    var connectionString = builder.Configuration.GetConnectionString("AzureStorage");
    return new Azure.Data.Tables.TableServiceClient(connectionString);
});

// CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://127.0.0.1:5500", "http://localhost:5500", "http://localhost:8080", "https://localhost:7183", "file://")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // ������� ��� SignalR
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();
app.MapHub<SmartDeliverySystem.Hubs.DeliveryTrackingHub>("/deliveryHub");

app.Run();
