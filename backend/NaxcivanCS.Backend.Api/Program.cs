// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using NaxcivanCS.Backend.Api.Contracts;
using NaxcivanCS.Backend.Api.Endpoints;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// PRD 111 - Security: rate limiting, HTTPS, input validation.
// MVP skeleton: konkret implementasiyalar Phase 4-də əlavə olunur.
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

WebApplication app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// PRD 98 - Game server registry, PRD 116 - texniki KPI monitorinqi.
app.MapHealthChecks("/health");
app.MapVersionEndpoints();
app.MapServerRegistryEndpoints();

app.Run();

/// <summary>Integration testlərinin WebApplicationFactory ilə istifadəsi üçün.</summary>
public partial class Program;
