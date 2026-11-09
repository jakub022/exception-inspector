using ExceptionInspector.Api.Endpoints;
using ExceptionInspector.Api.Services;
using ExceptionInspector.Core.Analysis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<IExceptionAnalyzer, ExceptionAnalyzer>();
builder.Services.AddScoped<IAnalysisService, ParserAnalysisService>();

var app = builder.Build();

app.UseExceptionHandler();
app.MapOpenApi();
app.MapAnalysisEndpoints();

app.Run();