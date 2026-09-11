// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

namespace NaxcivanCS.Backend.Api.Contracts;

/// <summary>PRD 102 - Client/server versiya uyğunluğu.</summary>
public sealed record VersionInfo(int ProtocolVersion, string GameVersion, string ContentVersion);

/// <summary>PRD 98 - Game server heartbeat məlumatı.</summary>
public sealed record ServerHeartbeat(
    string ServerId,
    string Region,
    string Ip,
    int Port,
    int Capacity,
    int Players,
    string Status,
    string Version);

/// <summary>PRD 98 - Registry-də saxlanan server qeydi.</summary>
public sealed record ServerRecord(
    string ServerId,
    string Region,
    string Ip,
    int Port,
    int Capacity,
    int Players,
    string Status,
    string Version,
    DateTimeOffset LastHeartbeat);
