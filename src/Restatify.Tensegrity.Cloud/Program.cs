using Restatify.Tensegrity.Cloud;

// Coverage exception: framework startup boilerplate is intentionally excluded from the coverage gate because it is
// generated ASP.NET host code, not product logic. This exception is documented here and should be revisited if the
// gateway behavior is moved into custom startup logic.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<IHiveClient>(_ =>
{
    var configured = builder.Configuration["Hive:BaseAddress"];
    return new HiveClient(!string.IsNullOrWhiteSpace(configured) ? new Uri(configured) : null);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new { status = "ok", gateway = "Tensegrity Cloud" }));

app.MapPost("/inference/generate", async (GenerateRequest request, IHiveClient hiveClient, CancellationToken cancellationToken) =>
    await GatewayRouter.HandleAsync(request, hiveClient, cancellationToken));

app.Run();

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public partial class Program { }
