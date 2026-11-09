using ExceptionInspector.Api.Endpoints;
using ExceptionInspector.Api.Data;
using ExceptionInspector.Api.Services;
using ExceptionInspector.Core.Analysis;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var allowedOrigins = builder.Configuration
  .GetSection("AllowedOrigins")
  .Get<string[]>() ?? [];

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddHealthChecks();
builder.Services.AddCors(options =>
{
  options.AddPolicy(
    "Client",
    policy =>
    {
      if (allowedOrigins.Length > 0)
      {
        policy.WithOrigins(allowedOrigins)
          .AllowAnyHeader()
          .AllowAnyMethod();
      }
    });
});
if (builder.Environment.IsDevelopment())
{
  builder.Services.AddDbContext<ExceptionInspectorDbContext>(options =>
    options.UseInMemoryDatabase("ExceptionInspector"));
}
else
{
  if (string.IsNullOrWhiteSpace(connectionString))
  {
    throw new InvalidOperationException(
      "ConnectionStrings:DefaultConnection must be configured outside Development.");
  }

  if (allowedOrigins.Length == 0)
  {
    throw new InvalidOperationException(
      "AllowedOrigins must contain the production client origin outside Development.");
  }

  if (string.IsNullOrWhiteSpace(builder.Configuration["ClientBaseUrl"]))
  {
    throw new InvalidOperationException(
      "ClientBaseUrl must be configured outside Development.");
  }

  builder.Services.AddDbContext<ExceptionInspectorDbContext>(options =>
    options.UseSqlServer(connectionString));
}
builder.Services.AddSingleton<IExceptionAnalyzer, ExceptionAnalyzer>();
builder.Services.AddScoped<IAnalysisService, ParserAnalysisService>();
builder.Services.AddScoped<IShareService, ShareService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors("Client");
app.MapOpenApi();
app.MapAnalysisEndpoints();
app.MapShareEndpoints(builder.Configuration);
app.MapHealthChecks("/health");

app.Run();