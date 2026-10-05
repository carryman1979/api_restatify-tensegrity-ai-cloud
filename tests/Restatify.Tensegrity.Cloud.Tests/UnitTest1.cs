using Restatify.Tensegrity.Cloud;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Restatify.Tensegrity.Cloud.Tests;

public class PolicyGuardTests
{
    private static readonly IHiveClient FakeHive = new FakeHiveClient();

    private sealed class FakeHiveClient : IHiveClient
    {
        public Task<HiveSubmitResult> SubmitAsync(GenerateRequest request, CancellationToken cancellationToken = default)
        {
            var slots = SlotRegistry.Resolve(request.ExpertSlotIds ?? Array.Empty<string>());
            return Task.FromResult(new HiveSubmitResult(true, slots, 0.8));
        }
    }

    [Theory]
    [InlineData("Offen", "OFFEN")]
    [InlineData(" offen ", "OFFEN")]
    [InlineData("Vertraulich", "VERTRAULICH")]
    [InlineData("Streng geheim", "STRENG_GEHEIM")]
    [InlineData("Nur lokal", "NUR_LOKAL")]
    [InlineData("unbekannt", "OFFEN")]
    public void LevelMapper_Maps_German_Levels_To_Hive_Enums(string input, string expected)
    {
        Assert.Equal(expected, ConfidentialityLevelMapper.ToHiveLevel(input));
    }

    [Fact]
    public void StrictSecret_IsRejected_ByPolicyGuard()
    {
        var result = PolicyGuard.IsRequestAllowed("StrictSecret");

        Assert.False(result);
    }

    [Fact]
    public void LocalOnly_IsRejected_ByPolicyGuard()
    {
        var result = PolicyGuard.IsRequestAllowed("NurLokal");

        Assert.False(result);
    }

    [Fact]
    public void Empty_Level_IsRejected_ByPolicyGuard()
    {
        var result = PolicyGuard.IsRequestAllowed("   ");

        Assert.False(result);
    }

    [Fact]
    public void Null_Level_IsRejected_ByPolicyGuard()
    {
        var result = PolicyGuard.IsRequestAllowed(null);

        Assert.False(result);
    }

    [Theory]
    [InlineData("Offen")]
    [InlineData("Vertraulich")]
    public void Allowed_Levels_AreAccepted_ByPolicyGuard(string level)
    {
        var result = PolicyGuard.IsRequestAllowed(level);

        Assert.True(result);
    }

    [Fact]
    public async Task GatewayRouter_Rejects_Null_Request()
    {
        var result = await GatewayRouter.HandleAsync(null!, FakeHive);
        var response = await ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
        Assert.Equal("Request body is required.", response.Body.GetProperty("error").GetString());
    }

    [Theory]
    [InlineData("StrengGeheim")]
    [InlineData("NurLokal")]
    [InlineData("unknown")]
    public async Task GatewayRouter_Rejects_Restricted_Request(string level)
    {
        var request = new GenerateRequest("req-9", level, "Geheime Anfrage");
        var result = await GatewayRouter.HandleAsync(request, FakeHive);
        var response = await ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
        Assert.False(response.Body.GetProperty("allowed").GetBoolean());
    }

    [Fact]
    public async Task GatewayRouter_Routes_Public_Request_To_Generalist()
    {
        var request = new GenerateRequest("req-10", "Offen", "Hallo", []);
        var result = await GatewayRouter.HandleAsync(request, FakeHive);
        var response = await ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Equal("generalist", response.Body.GetProperty("route").GetString());
        Assert.Equal(5, response.Body.GetProperty("promptLength").GetInt32());
        Assert.Empty(response.Body.GetProperty("selectedExpertSlotIds").EnumerateArray());
    }

    [Theory]
    [InlineData("Vertraulich", "generalist-with-redaction")]
    [InlineData(" vertraulich ", "generalist-with-redaction")]
    public async Task GatewayRouter_Routes_Confidential_Request_Through_Redaction(
        string level,
        string expectedRoute)
    {
        var request = new GenerateRequest("req-11", level, "Sensible Anfrage");
        var result = await GatewayRouter.HandleAsync(request, FakeHive);
        var response = await ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Equal(expectedRoute, response.Body.GetProperty("route").GetString());
    }

    [Fact]
    public async Task GatewayRouter_Routes_Selected_Experts_After_Generalist()
    {
        var request = new GenerateRequest(
            "req-12",
            "Offen",
            "Fachfrage",
            [" legal ", "finance", "legal", " "]);
        var result = await GatewayRouter.HandleAsync(request, FakeHive);
        var response = await ExecuteAsync(result);
        var selectedExperts = response.Body.GetProperty("selectedExpertSlotIds")
            .EnumerateArray()
            .Select(slotId => slotId.GetString()!)
            .ToArray();

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Equal("generalist-with-experts", response.Body.GetProperty("route").GetString());
        Assert.Equal(["finance", "legal"], selectedExperts);
    }

    [Fact]
    public async Task GatewayRouter_Rejects_Unknown_Or_Inactive_Slots()
    {
        var request = new GenerateRequest(
            "req-14",
            "Offen",
            "Fachfrage",
            ["medical", "unknown"]);
        var result = await GatewayRouter.HandleAsync(request, FakeHive);
        var response = await ExecuteAsync(result);
        var unknownSlots = response.Body.GetProperty("unknownSlotIds")
            .EnumerateArray()
            .Select(slotId => slotId.GetString()!)
            .ToArray();

        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
        Assert.Equal(["medical", "unknown"], unknownSlots);
    }

    [Fact]
    public async Task GatewayRouter_Combines_Redaction_And_Expert_Routing()
    {
        var request = new GenerateRequest("req-13", "Vertraulich", "Sensible Fachfrage", ["legal"]);
        var result = await GatewayRouter.HandleAsync(request, FakeHive);
        var response = await ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Equal(
            "generalist-with-redaction-and-experts",
            response.Body.GetProperty("route").GetString());
    }

    private static async Task<(int StatusCode, JsonElement Body)> ExecuteAsync(IResult result)
    {
        await using var responseBody = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = responseBody;
        var services = new ServiceCollection();
        services.AddLogging();
        services.ConfigureHttpJsonOptions(_ => { });
        context.RequestServices = services.BuildServiceProvider();
        await result.ExecuteAsync(context);
        responseBody.Position = 0;
        using var document = await JsonDocument.ParseAsync(responseBody);

        return (context.Response.StatusCode, document.RootElement.Clone());
    }
}
