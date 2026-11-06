using ExceptionInspector.Api.Endpoints;
using ExceptionInspector.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddScoped<IAnalysisService, MockAnalysisService>();

var app = builder.Build();

app.UseExceptionHandler();
app.MapOpenApi();
app.MapAnalysisEndpoints();

app.Run();