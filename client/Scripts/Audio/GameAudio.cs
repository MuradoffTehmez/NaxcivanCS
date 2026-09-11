// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using Godot;
using NaxcivanCS.Shared.Enums;

namespace NaxcivanCS.Client.Audio;

/// <summary>
/// PRD 83 - Səs oxutma qatı.
///
/// <para>
/// <b>Spatial audio rəqabətdə kritikdir:</b> oyunçu düşmənin harada olduğunu
/// qulaqla təyin edə bilməlidir. Ona görə başqa oyunçuların səsləri
/// <see cref="AudioStreamPlayer3D"/> ilə mövqedən səslənir; yalnız oyunçunun
/// öz silahı və hit marker 2D-dir (həmişə eyni səviyyədə).
/// </para>
///
/// <para>
/// 3D oxuducular hovuzdan verilir — spray zamanı saniyədə 10 atəş üçün hər
/// dəfə yeni node yaratmaq GC yükü yaradar.
/// </para>
/// </summary>
public sealed partial class GameAudio : Node3D
{
    /// <summary>Eyni anda səslənə bilən 3D səs sayı.</summary>
    private const int PoolSize = 24;

    /// <summary>Atəş bu məsafəyə qədər eşidilir (PRD 83 — taktiki məlumat).</summary>
    private const float GunshotRange = 70f;

    /// <summary>Addım səsinin eşidilmə məsafəsi. Rəqabət balansı üçün məhduddur.</summary>
    private const float FootstepRange = 24f;

    private readonly List<AudioStreamPlayer3D> _pool = new();
    private readonly Dictionary<SurfaceMaterial, AudioStreamWav> _footsteps = new();

    private AudioStreamPlayer? _localGunshot;
    private AudioStreamPlayer? _localUi;

    private AudioStreamWav? _rifleShot;
    private AudioStreamWav? _dryFire;
    private AudioStreamWav? _magazineOut;
    private AudioStreamWav? _magazineIn;
    private AudioStreamWav? _worldImpact;
    private AudioStreamWav? _fleshImpact;
    private AudioStreamWav? _hitMarker;
    private AudioStreamWav? _killMarker;

    private int _nextPoolIndex;

    public override void _Ready()
    {
        _rifleShot = ProceduralAudio.RifleShot();
        _dryFire = ProceduralAudio.DryFire();
        _magazineOut = ProceduralAudio.MagazineOut();
        _magazineIn = ProceduralAudio.MagazineIn();
        _worldImpact = ProceduralAudio.WorldImpact();
        _fleshImpact = ProceduralAudio.FleshImpact();
        _hitMarker = ProceduralAudio.HitMarker(killed: false);
        _killMarker = ProceduralAudio.HitMarker(killed: true);

        foreach (SurfaceMaterial surface in Enum.GetValues<SurfaceMaterial>())
        {
            _footsteps[surface] = ProceduralAudio.Footstep(surface);
        }

        for (int i = 0; i < PoolSize; i++)
        {
            var player = new AudioStreamPlayer3D
            {
                Name = $"Spatial_{i}",
                AttenuationModel = AudioStreamPlayer3D.AttenuationModelEnum.InverseSquareDistance,
                MaxDistance = GunshotRange,
            };

            AddChild(player);
            _pool.Add(player);
        }

        _localGunshot = new AudioStreamPlayer { Name = "LocalGunshot", VolumeDb = -4f };
        AddChild(_localGunshot);

        _localUi = new AudioStreamPlayer { Name = "LocalUi", VolumeDb = -8f };
        AddChild(_localUi);

        GD.Print($"[GameAudio] {_footsteps.Count} səth səsi + silah səsləri sintez edildi");

        if (ParseDumpDirectory() is { } dumpDirectory)
        {
            DumpSynthesizedAudio(dumpDirectory);
        }
    }

    /// <summary>
    /// Debug: sintez edilmiş səsləri WAV faylı kimi yazır.
    /// Səsin həqiqətən yarandığını yoxlamaq üçün — oyunu işə salmadan
    /// dalğa formasını analiz etmək olur.
    /// </summary>
    private void DumpSynthesizedAudio(string directory)
    {
        DirAccess.MakeDirRecursiveAbsolute(directory);

        var streams = new Dictionary<string, AudioStreamWav?>
        {
            ["rifle_shot"] = _rifleShot,
            ["dry_fire"] = _dryFire,
            ["magazine_out"] = _magazineOut,
            ["magazine_in"] = _magazineIn,
            ["world_impact"] = _worldImpact,
            ["flesh_impact"] = _fleshImpact,
            ["hit_marker"] = _hitMarker,
            ["kill_marker"] = _killMarker,
        };

        foreach ((SurfaceMaterial surface, AudioStreamWav stream) in _footsteps)
        {
            streams[$"footstep_{surface.ToString().ToLowerInvariant()}"] = stream;
        }

        foreach ((string name, AudioStreamWav? stream) in streams)
        {
            stream?.SaveToWav(Path.Combine(directory, name + ".wav"));
        }

        GD.Print($"[GameAudio] {streams.Count} səs faylı yazıldı: {directory}");
    }

    private static string? ParseDumpDirectory()
    {
        string[] args = OS.GetCmdlineUserArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--dump-audio")
            {
                return args[i + 1];
            }
        }

        return null;
    }

    /// <summary>Oyunçunun öz atəşi — 2D, həmişə eyni səviyyədə.</summary>
    public void PlayLocalShot()
    {
        if (_localGunshot is not null && _rifleShot is not null)
        {
            _localGunshot.Stream = _rifleShot;
            _localGunshot.PitchScale = (float)GD.RandRange(0.97, 1.03);
            _localGunshot.Play();
        }
    }

    /// <summary>Başqa oyunçunun atəşi — mövqedən səslənir.</summary>
    public void PlayRemoteShot(Vector3 position)
        => PlaySpatial(_rifleShot, position, volumeDb: 0f, pitchJitter: 0.04f);

    public void PlayDryFire() => PlayLocalUi(_dryFire);

    public void PlayReloadStart(Vector3 position, bool isLocal)
    {
        if (isLocal)
        {
            PlayLocalUi(_magazineOut, volumeDb: -6f);
            return;
        }

        PlaySpatial(_magazineOut, position, volumeDb: -6f, pitchJitter: 0.03f, maxDistance: FootstepRange);
    }

    public void PlayReloadEnd(Vector3 position, bool isLocal)
    {
        if (isLocal)
        {
            PlayLocalUi(_magazineIn, volumeDb: -6f);
            return;
        }

        PlaySpatial(_magazineIn, position, volumeDb: -6f, pitchJitter: 0.03f, maxDistance: FootstepRange);
    }

    /// <summary>Güllənin dəydiyi yerdən səs — hədəfin harada olduğunu bildirir.</summary>
    public void PlayImpact(Vector3 position, bool hitPlayer)
        => PlaySpatial(hitPlayer ? _fleshImpact : _worldImpact, position, volumeDb: -3f, pitchJitter: 0.08f);

    /// <summary>PRD 78 - Vurulma təsdiqi, yalnız atıcıya.</summary>
    public void PlayHitMarker(bool killed) => PlayLocalUi(killed ? _killMarker : _hitMarker, volumeDb: -6f);

    /// <summary>PRD 84 - Səthə uyğun addım səsi.</summary>
    public void PlayFootstep(Vector3 position, SurfaceMaterial surface, bool isLocal)
    {
        if (!_footsteps.TryGetValue(surface, out AudioStreamWav? stream))
        {
            return;
        }

        if (isLocal)
        {
            // Öz addımların 2D-dir, amma sakit — düşmənin addımını örtməməlidir.
            PlayLocalUi(stream, volumeDb: -14f, pitchJitter: 0.09f);
            return;
        }

        PlaySpatial(stream, position, volumeDb: -2f, pitchJitter: 0.09f, maxDistance: FootstepRange);
    }

    private void PlaySpatial(
        AudioStreamWav? stream,
        Vector3 position,
        float volumeDb,
        float pitchJitter,
        float maxDistance = GunshotRange)
    {
        if (stream is null || _pool.Count == 0)
        {
            return;
        }

        AudioStreamPlayer3D player = NextFreePlayer();

        player.Stream = stream;
        player.GlobalPosition = position;
        player.VolumeDb = volumeDb;
        player.MaxDistance = maxDistance;
        player.PitchScale = 1f + (float)GD.RandRange(-pitchJitter, pitchJitter);
        player.Play();
    }

    private void PlayLocalUi(AudioStreamWav? stream, float volumeDb = -8f, float pitchJitter = 0f)
    {
        if (_localUi is null || stream is null)
        {
            return;
        }

        _localUi.Stream = stream;
        _localUi.VolumeDb = volumeDb;
        _localUi.PitchScale = pitchJitter > 0f ? 1f + (float)GD.RandRange(-pitchJitter, pitchJitter) : 1f;
        _localUi.Play();
    }

    /// <summary>Boş oxuducu tapır; hamısı məşğuldursa ən köhnəsini kəsir.</summary>
    private AudioStreamPlayer3D NextFreePlayer()
    {
        foreach (AudioStreamPlayer3D candidate in _pool)
        {
            if (!candidate.Playing)
            {
                return candidate;
            }
        }

        AudioStreamPlayer3D oldest = _pool[_nextPoolIndex];
        _nextPoolIndex = (_nextPoolIndex + 1) % _pool.Count;
        return oldest;
    }
}
