// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Collections.Concurrent;
using NaxcivanCS.Backend.Api.Contracts;

namespace NaxcivanCS.Backend.Api.Endpoints;

/// <summary>
/// PRD 98, 99 - Game server registry.
/// MVP-də in-memory; Phase 5-də Redis-ə köçürüləcək (PRD 91).
/// </summary>
public static class ServerRegistryEndpoints
{
    private static readonly ConcurrentDictionary<string, ServerRecord> Servers = new(StringComparer.Ordinal);

    /// <summary>Heartbeat gəlmirsə server ölü sayılır.</summary>
    private static readonly TimeSpan HeartbeatTimeout = TimeSpan.FromSeconds(30);

    public static IEndpointRouteBuilder MapServerRegistryEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/v1/servers");

        group.MapPost("/heartbeat", (ServerHeartbeat heartbeat) =>
        {
            if (string.IsNullOrWhiteSpace(heartbeat.ServerId))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["serverId"] = ["serverId boş ola bilməz"],
                });
            }

            Servers[heartbeat.ServerId] = new ServerRecord(
                heartbeat.ServerId,
                heartbeat.Region,
                heartbeat.Ip,
                heartbeat.Port,
                heartbeat.Capacity,
                heartbeat.Players,
                heartbeat.Status,
                heartbeat.Version,
                DateTimeOffset.UtcNow);

            return Results.Accepted();
        }).WithName("PostServerHeartbeat");

        group.MapGet("/", () =>
        {
            DateTimeOffset cutoff = DateTimeOffset.UtcNow - HeartbeatTimeout;
            return Servers.Values.Where(s => s.LastHeartbeat >= cutoff).ToArray();
        }).WithName("ListServers");

        return app;
    }
}
