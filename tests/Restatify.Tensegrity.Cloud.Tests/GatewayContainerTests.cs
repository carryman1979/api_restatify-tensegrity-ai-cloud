using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Restatify.Tensegrity.Cloud.Tests;

// Echte Container-Integration: startet den Hive in einem Docker-Container und prüft
// die Kette App-Request → Cloud → Hive end-to-end. Läuft nur, wenn die Umgebungsvariable
// TENGRITY_CONTAINER_TESTS=1 gesetzt ist, damit der Standardlauf ohne Docker grün bleibt.
public sealed class GatewayContainerTests : IAsyncLifetime
{
    private IContainer? _hive;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    public async Task InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("TENGRITY_CONTAINER_TESTS") != "1")
        {
            return;
        }

        var hiveDir = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "api_restatify-tensegrity-ai-hive"));

        _hive = new ContainerBuilder()
            .WithImage("python:3.14-slim")
            .WithBindMount(hiveDir, "/app")
            .WithWorkingDirectory("/app")
            .WithCommand("python", "-m", "hive", "--port", "50051")
            .WithPortBinding(50051, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Hive listening"))
            .Build();
        await _hive.StartAsync();

        var hiveUrl = new Uri($"http://127.0.0.1:{_hive.GetMappedPublicPort(50051)}");
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IHiveClient>(new HiveClient(hiveUrl));
            });
        });
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        if (_hive is not null)
        {
            await _hive.DisposeAsync();
        }
    }

    [Fact]
    public async Task Cloud_And_Hive_Run_Together_In_Containers()
    {
        if (_client is null)
        {
            return; // explizit übersprungen: TENGRITY_CONTAINER_TESTS ist nicht gesetzt
        }

        var response = await _client.PostAsJsonAsync("/inference/generate", new
        {
            RequestId = "container-1",
            ConfidentialityLevel = "Offen",
            Prompt = "Container-Test",
            ExpertSlotIds = new[] { "legal" }
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body.GetProperty("allowed").GetBoolean());
        Assert.True(body.GetProperty("hiveAccepted").GetBoolean());
        Assert.Equal(
            new[] { "legal" },
            body.GetProperty("hiveResolvedSlots").EnumerateArray()
                .Select(slotId => slotId.GetString()!)
                .ToArray());
    }
}
