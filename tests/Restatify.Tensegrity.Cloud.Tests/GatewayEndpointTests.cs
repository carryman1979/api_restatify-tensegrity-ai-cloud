using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Restatify.Tensegrity.Cloud.Tests;

public sealed class GatewayEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public GatewayEndpointTests(WebApplicationFactory<Program> factory)
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
    public async Task Generate_Routes_Selected_Experts()
    {
        var response = await _client.PostAsJsonAsync("/inference/generate", new
        {
            RequestId = "component-1",
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

    [Theory]
    [InlineData("StrengGeheim")]
    [InlineData("NurLokal")]
    public async Task Generate_Rejects_Restricted_Confidentiality(string level)
    {
        var response = await _client.PostAsJsonAsync("/inference/generate", new
        {
            RequestId = "component-2",
            ConfidentialityLevel = level,
            Prompt = "Nicht an die Cloud senden"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Generate_Rejects_Unknown_Experts()
    {
        var response = await _client.PostAsJsonAsync("/inference/generate", new
        {
            RequestId = "component-3",
            ConfidentialityLevel = "Offen",
            Prompt = "Fachfrage",
            ExpertSlotIds = new[] { "unknown" }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}