using Ruth.Host.Endpoints;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapHealthEndpoints();

await app.RunAsync();
