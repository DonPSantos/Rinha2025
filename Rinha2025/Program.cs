using Microsoft.AspNetCore.Mvc;
using Rinha2025;
using Rinha2025.Clients;
using Rinha2025.DTO;
using Rinha2025.Models;
using Rinha2025.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<PaymentProcessorDefaultClient>(client =>
{
    client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("PROCESSOR_DEFAULT_URL"));
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient<PaymentProcessorFallbackClient>(client =>
{
    client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("PROCESSOR_FALLBACK_URL"));
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddSingleton<PaymentQueue>();
builder.Services.AddSingleton<PaymentRepository>();
builder.Services.AddHostedService<PaymentWorker>();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health");

app.MapPost("/payments", ([FromBody] PostPaymentsRequest request, [FromServices] PaymentQueue queue) =>
{
    var processorRequest = new ProcessorRequest
    {
        Amount = request.Amount,
        CorrelationId = request.CorrelationId,
        RequestedAt = DateTime.UtcNow
    };

    queue.Enqueue(processorRequest);

    return Results.Ok("Payments");
});

app.MapGet("/payments-summary", ([FromQuery] DateTime from, [FromQuery] DateTime to, [FromServices] PaymentRepository repository) =>
{
});

var dataDir = Path.Combine(Directory.GetCurrentDirectory(), "Data");
if (!Directory.Exists(dataDir))
{
    Directory.CreateDirectory(dataDir);
}

app.Run();