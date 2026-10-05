using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Restatify.Tensegrity.Cloud.Tests;

public sealed class GatewayIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public GatewayIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IHiveClient>(new FakeHiveClient());
            });
        }).CreateClient();
    }

    private sealed class FakeHiveClient : IHiveClient
    {
        public Task<HiveSubmitResult> SubmitAsync(GenerateRequest request, CancellationToken cancellationToken = default)
        {
            var slots = SlotRegistry.Resolve(request.ExpertSlotIds ?? Array.Empty<string>());
            return Task.FromResult(new HiveSubmitResult(true, slots, 0.8));
        }
    }

    [Fact]
    public async Task Generate_With_Valid_Expert_Slots_Passes_Trust_And_Routing()
    {
        var response = await _client.PostAsJsonAsync("/inference/generate", new
        {
            RequestId = "integration-1",
            ConfidentialityLevel = "Offen",
            Prompt = "Fachfrage",
            ExpertSlotIds = new[] { "legal", "finance" }
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("generalist-with-experts", body.GetProperty("route").GetString());
        Assert.Equal(
            new[] { "finance", "legal" },
            body.GetProperty("selectedExpertSlotIds").EnumerateArray()
                .Select(slotId => slotId.GetString()!)
                .ToArray());
    }

    [Fact]
    public async Task Generate_With_Unknown_Slot_Is_Rejected_Before_Trust_Check()
    {
        var response = await _client.PostAsJsonAsync("/inference/generate", new
        {
            RequestId = "integration-2",
            ConfidentialityLevel = "Offen",
            Prompt = "Fachfrage",
            ExpertSlotIds = new[] { "unknown" }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Generate_With_Strict_Level_Is_Rejected_Before_Routing()
    {
        var response = await _client.PostAsJsonAsync("/inference/generate", new
        {
            RequestId = "integration-3",
            ConfidentialityLevel = "StrengGeheim",
            Prompt = "Vertrauliche Fachfrage",
            ExpertSlotIds = new[] { "legal" }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
