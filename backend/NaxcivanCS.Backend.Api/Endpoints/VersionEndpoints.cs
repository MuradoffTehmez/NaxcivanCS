using NaxcivanCS.Backend.Api.Contracts;
using NaxcivanCS.Shared.Constants;

namespace NaxcivanCS.Backend.Api.Endpoints;

public static class VersionEndpoints
{
    public static IEndpointRouteBuilder MapVersionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/version", () => new VersionInfo(
                GameConstants.ProtocolVersion,
                GameConstants.GameVersion,
                GameConstants.ContentVersion))
            .WithName("GetVersion");

        return app;
    }
}
