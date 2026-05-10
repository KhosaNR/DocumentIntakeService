using DocumentIntake.Api.Endpoints;
using DocumentIntake.Api.Middleware;
using DocumentIntake.Application.Workers;
using DocumentIntake.Domain.Interfaces;
using DocumentIntake.Infrastructure.Messaging;
using DocumentIntake.Infrastructure.Repositories;
using DocumentIntake.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddSingleton<IStorageService, InMemoryStorageService>();
builder.Services.AddSingleton<IDocumentRepository, InMemoryDocumentRepository>();
builder.Services.AddSingleton<IMessageQueue, InMemoryMessageQueue>();
builder.Services.AddHostedService<DocumentProcessingWorker>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDocumentEndpoints();

app.Run();