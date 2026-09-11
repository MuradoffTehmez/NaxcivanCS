# PRD 88, 123 - ASP.NET Core backend API konteyneri.

ARG DOTNET_SDK_VERSION=8.0.425
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_VERSION} AS build
WORKDIR /src

COPY Directory.Build.props global.json ./
COPY shared/ shared/
COPY backend/ backend/

RUN dotnet restore backend/NaxcivanCS.Backend.Api/NaxcivanCS.Backend.Api.csproj \
 && dotnet publish backend/NaxcivanCS.Backend.Api/NaxcivanCS.Backend.Api.csproj \
      -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy AS runtime
WORKDIR /app

RUN useradd --create-home --uid 10002 naxcivan
COPY --from=build /app/publish .
USER naxcivan

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "NaxcivanCS.Backend.Api.dll"]
