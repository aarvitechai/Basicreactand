using RedStarVibe.Api.Extensions;
using RedStarVibe.Application;
using RedStarVibe.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration, builder.Environment)
    .AddApiServices(builder.Configuration);

var app = builder.Build();

await app.UseApiPipelineAsync();

await app.RunAsync();

/// <summary>Exposed so integration tests can use <c>WebApplicationFactory&lt;Program&gt;</c>.</summary>
public partial class Program;
