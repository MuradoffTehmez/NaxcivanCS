// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Text.Json;
using NaxcivanCS.Shared.Config;
using NaxcivanCS.Shared.Constants;
using NaxcivanCS.Shared.Enums;
using NaxcivanCS.Shared.Gameplay;
using Xunit;

namespace NaxcivanCS.Shared.Tests;

/// <summary>PRD 10, 148 - Round dəyərləri build dəyişmədən config-dən gəlməlidir.</summary>
public sealed class ServerConfigTests
{
    [Fact]
    public void Defaults_MatchGameConstants()
    {
        RoundTimings timings = RoundTimings.Defaults;

        Assert.Equal(GameConstants.FreezeTimeSeconds, timings.FreezeTimeSeconds);
        Assert.Equal(GameConstants.RoundTimeSeconds, timings.RoundTimeSeconds);
        Assert.Equal(GameConstants.BombTimerSeconds, timings.BombTimerSeconds);
        Assert.Equal(GameConstants.PlantTimeSeconds, timings.PlantTimeSeconds);
        Assert.Equal(GameConstants.DefuseTimeWithKitSeconds, timings.DefuseTimeWithKitSeconds);
    }

    [Fact]
    public void Parse_ReadsRoundSection()
    {
        ServerConfig config = ServerConfig.Parse("""
            {
              "serverName": "Test",
              "map": "NC_Duzdag",
              "friendlyFire": true,
              "round": { "freezeTimeSeconds": 3, "bombTimerSeconds": 35, "plantTimeSeconds": 2 }
            }
            """);

        Assert.Equal("Test", config.ServerName);
        Assert.Equal("NC_Duzdag", config.Map);
        Assert.True(config.FriendlyFire);
        Assert.Equal(3f, config.Round.FreezeTimeSeconds);
        Assert.Equal(35f, config.Round.BombTimerSeconds);
        Assert.Equal(2f, config.Round.PlantTimeSeconds);
    }

    /// <summary>Yarımçıq config qalan taymerləri sıfırlamamalıdır.</summary>
    [Fact]
    public void Parse_MissingKeys_KeepDefaults()
    {
        ServerConfig config = ServerConfig.Parse("""{ "round": { "freezeTimeSeconds": 2 } }""");

        Assert.Equal(2f, config.Round.FreezeTimeSeconds);
        Assert.Equal(GameConstants.RoundTimeSeconds, config.Round.RoundTimeSeconds);
        Assert.Equal(GameConstants.DefuseTimeSeconds, config.Round.DefuseTimeSeconds);
    }

    [Fact]
    public void Parse_WithoutRoundSection_UsesDefaults()
    {
        ServerConfig config = ServerConfig.Parse("""{ "serverName": "Bare" }""");

        Assert.Equal(GameConstants.FreezeTimeSeconds, config.Round.FreezeTimeSeconds);
        Assert.Equal(GameConstants.BombTimerSeconds, config.Round.BombTimerSeconds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Parse_NonPositiveTimer_IsRejected(float value)
    {
        string json = $$"""{ "round": { "roundTimeSeconds": {{value.ToString(System.Globalization.CultureInfo.InvariantCulture)}} } }""";

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => ServerConfig.Parse(json));
        Assert.Contains("RoundTimeSeconds", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_MalformedJson_Throws()
        => Assert.Throws<JsonException>(() => ServerConfig.Parse("{ not json"));

    [Fact]
    public void LoadFirstAvailable_NoFile_ReturnsDefaults()
    {
        ServerConfig config = ServerConfig.LoadFirstAvailable(
            new[] { Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.json") });

        Assert.Equal(GameConstants.RoundTimeSeconds, config.Round.RoundTimeSeconds);
    }

    /// <summary>Repodakı real config faylı sxemə uyğun olmalıdır.</summary>
    [Fact]
    public void RepositoryConfig_IsValid()
    {
        string path = RepositoryFile("config/server_default.json");
        ServerConfig config = ServerConfig.Parse(File.ReadAllText(path));

        Assert.Equal(GameConstants.FreezeTimeSeconds, config.Round.FreezeTimeSeconds);
        Assert.Equal(GameConstants.RoundTimeSeconds, config.Round.RoundTimeSeconds);
        Assert.Equal(GameConstants.BombTimerSeconds, config.Round.BombTimerSeconds);
        Assert.Equal(GameConstants.DefuseTimeWithKitSeconds, config.Round.DefuseTimeWithKitSeconds);
    }

    /// <summary>Config-dəki taymer həqiqətən round fazasına tətbiq olunur.</summary>
    [Fact]
    public void MatchDirector_UsesConfiguredTimings()
    {
        var timings = new RoundTimings { FreezeTimeSeconds = 1f, BuyTimeSeconds = 2f };
        var director = new MatchDirector(timings: timings);

        director.Tick(0.016f, 1, 1, connectedPlayers: 2);

        Assert.Equal(RoundPhase.FreezeTime, director.Phase);
        Assert.Equal(1f, MatchRules.PhaseDuration(RoundPhase.FreezeTime, timings));
        Assert.Equal(2f, MatchRules.PhaseDuration(RoundPhase.BuyTime, timings));
        Assert.True(director.PhaseTimeRemaining <= 1f);
    }

    [Fact]
    public void BombDirector_UsesConfiguredPlantTime()
    {
        var timings = new RoundTimings { PlantTimeSeconds = 1f };
        var bomb = new BombDirector(timings);
        System.Numerics.Vector3 site = BlockoutMap.Sites[0].Center with { Y = 0.1f };
        bomb.BeginRound(1, site);

        var attacker = new BombInteractor(1, Team.Alpha, true, site, true, false);
        BombEvent last = BombEvent.None;
        for (int i = 0; i < 70; i++)
        {
            last = bomb.Tick(GameConstants.ServerTickIntervalSeconds, RoundPhase.Active, new[] { attacker });
            if (last == BombEvent.Planted)
            {
                break;
            }
        }

        Assert.Equal(BombEvent.Planted, last);
        Assert.Equal(BombState.Planted, bomb.State);
    }

    private static string RepositoryFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Repo faylı tapılmadı: {relativePath}");
    }
}
