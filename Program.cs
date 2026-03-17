using Microsoft.EntityFrameworkCore;
using RetailForecast.Data;
using RetailForecast.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<RetailForecastDbContext>(options =>
    options.UseNpgsql(builder.Configuration
        .GetConnectionString("DefaultConnection"))
           .UseSnakeCaseNamingConvention());

// Регистрация сервисов
builder.Services.AddScoped<DatasetService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ModelService>();
builder.Services.AddScoped<KpiService>();
builder.Services.AddScoped<TrainingRunService>();
builder.Services.AddScoped<ForecastService>();
builder.Services.AddScoped<TrainingMetricService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.MapControllers();

app.Run();