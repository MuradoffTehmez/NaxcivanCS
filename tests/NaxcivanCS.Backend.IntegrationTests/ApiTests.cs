// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using NaxcivanCS.Backend.Api.Contracts;
using NaxcivanCS.Shared.Constants;
using Xunit;

namespace NaxcivanCS.Backend.IntegrationTests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Production");
}

public sealed class ApiTests : IClassFixture<ApiFactory>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        using HttpClient client = _factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Version_MatchesSharedContract()
    {
        using HttpClient client = _factory.CreateClient();
        VersionInfo? version = await client.GetFromJsonAsync<VersionInfo>("/api/v1/version");
        Assert.Equal(new VersionInfo(GameConstants.ProtocolVersion,
            GameConstants.GameVersion, GameConstants.ContentVersion), version);
    }

    [Fact]
    public async Task Heartbeat_RegistersAndUpdatesOneServer()
    {
        using HttpClient client = _factory.CreateClient();
        var heartbeat = new ServerHeartbeat(Guid.NewGuid().ToString("N"), "az", "127.0.0.1",
            27015, 10, 1, "active", GameConstants.GameVersion);
        DateTimeOffset before = DateTimeOffset.UtcNow;
        using HttpResponseMessage created = await client.PostAsJsonAsync("/api/v1/servers/heartbeat", heartbeat);
        Assert.Equal(HttpStatusCode.Accepted, created.StatusCode);
        ServerRecord[] records = (await client.GetFromJsonAsync<ServerRecord[]>("/api/v1/servers/"))!;
        ServerRecord record = Assert.Single(records, r => r.ServerId == heartbeat.ServerId);
        Assert.Equal(heartbeat.Region, record.Region);
        Assert.Equal(heartbeat.Ip, record.Ip);
        Assert.Equal(heartbeat.Port, record.Port);
        Assert.Equal(heartbeat.Capacity, record.Capacity);
        Assert.Equal(heartbeat.Version, record.Version);
        Assert.InRange(record.LastHeartbeat, before, DateTimeOffset.UtcNow);

        using HttpResponseMessage updated = await client.PostAsJsonAsync("/api/v1/servers/heartbeat",
            heartbeat with { Players = 5, Status = "full" });
        Assert.Equal(HttpStatusCode.Accepted, updated.StatusCode);
        records = (await client.GetFromJsonAsync<ServerRecord[]>("/api/v1/servers/"))!;
        record = Assert.Single(records, r => r.ServerId == heartbeat.ServerId);
        Assert.Equal(5, record.Players);
        Assert.Equal("full", record.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Heartbeat_BlankId_ReturnsValidationProblem(string? serverId)
    {
        using HttpClient client = _factory.CreateClient();
        var heartbeat = new ServerHeartbeat(serverId!, "az", "127.0.0.1", 27015, 10, 0, "active", "test");
        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/servers/heartbeat", heartbeat);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("serverId", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("{broken", "application/json", HttpStatusCode.BadRequest)]
    [InlineData("null", "application/json", HttpStatusCode.BadRequest)]
    [InlineData("{}", "text/plain", HttpStatusCode.UnsupportedMediaType)]
    public async Task Heartbeat_InvalidBody_IsRejected(string body, string mediaType, HttpStatusCode expected)
    {
        using HttpClient client = _factory.CreateClient();
        using var content = new StringContent(body, Encoding.UTF8, mediaType);
        using HttpResponseMessage response = await client.PostAsync("/api/v1/servers/heartbeat", content);
        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task UnknownRoute_ReturnsNotFound()
    {
        using HttpClient client = _factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/api/v1/missing");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
