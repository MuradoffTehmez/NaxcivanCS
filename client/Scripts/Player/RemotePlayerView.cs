using Godot;
using NaxcivanCS.Shared.Constants;
using NaxcivanCS.Shared.Gameplay;
using NaxcivanCS.Shared.Net;

namespace NaxcivanCS.Client.Player;

/// <summary>
/// PRD 44 - Digər oyunçular üçün snapshot interpolation.
///
/// Uzaq oyunçular birbaşa son snapshot-a "atılmır" — client vaxtı bir qədər
/// geridə saxlayır (<see cref="GameConstants.SnapshotInterpolationDelayMs"/>) və
/// iki snapshot arasında interpolyasiya edir. Bu, network jitter-i gizlədir.
/// </summary>
public sealed partial class RemotePlayerView : Node3D
{
    private readonly record struct Sample(double ServerTimeMs, Vector3 Position, float Yaw, bool IsCrouching);

    private readonly List<Sample> _samples = new();

    /// <summary>Saxlanan maksimum snapshot sayı (~1 saniyə, 32 Hz-də).</summary>
    private const int MaxSamples = 40;

    private MeshInstance3D? _body;

    public int PeerId { get; private set; }

    public int Health { get; private set; } = GameConstants.MaxHealth;

    public bool IsAlive => Health > 0;

    public static RemotePlayerView Create(int peerId)
    {
        var view = new RemotePlayerView { Name = $"Remote_{peerId}", PeerId = peerId };
        return view;
    }

    public override void _Ready()
    {
        _body = new MeshInstance3D
        {
            Name = "Body",
            Mesh = new CapsuleMesh
            {
                Radius = HitScan.PlayerRadius,
                Height = HitScan.StandingHeight,
            },
            Position = new Vector3(0f, HitScan.StandingHeight / 2f, 0f),
        };

        AddChild(_body);
    }

    /// <summary>Serverdən gələn hər snapshot-da çağırılır.</summary>
    public void Push(PlayerSnapshot snapshot, double serverTimeMs)
    {
        Health = snapshot.Health;
        Visible = IsAlive;

        _samples.Add(new Sample(
            serverTimeMs,
            new Vector3(snapshot.Position.X, snapshot.Position.Y, snapshot.Position.Z),
            snapshot.Yaw,
            snapshot.IsCrouching));

        if (_samples.Count > MaxSamples)
        {
            _samples.RemoveAt(0);
        }
    }

    /// <summary>
    /// Verilmiş render vaxtına görə mövqeyi interpolyasiya edir.
    /// <paramref name="renderTimeMs"/> həmişə serverin son vaxtından
    /// <see cref="GameConstants.SnapshotInterpolationDelayMs"/> qədər geridədir.
    /// </summary>
    public void Interpolate(double renderTimeMs)
    {
        if (_samples.Count == 0)
        {
            return;
        }

        if (_samples.Count == 1 || renderTimeMs <= _samples[0].ServerTimeMs)
        {
            ApplySample(_samples[0]);
            return;
        }

        if (renderTimeMs >= _samples[^1].ServerTimeMs)
        {
            ApplySample(_samples[^1]);
            return;
        }

        for (int i = 0; i < _samples.Count - 1; i++)
        {
            Sample older = _samples[i];
            Sample newer = _samples[i + 1];

            if (renderTimeMs < older.ServerTimeMs || renderTimeMs > newer.ServerTimeMs)
            {
                continue;
            }

            double span = newer.ServerTimeMs - older.ServerTimeMs;
            float t = span <= 0 ? 0f : (float)((renderTimeMs - older.ServerTimeMs) / span);

            GlobalPosition = older.Position.Lerp(newer.Position, t);

            // LerpAngle 360°/0° keçidini düzgün idarə edir.
            Rotation = new Vector3(
                0f,
                Mathf.LerpAngle(Mathf.DegToRad(older.Yaw), Mathf.DegToRad(newer.Yaw), t),
                0f);

            UpdateHeight(t < 0.5f ? older.IsCrouching : newer.IsCrouching);
            return;
        }
    }

    private void ApplySample(Sample sample)
    {
        GlobalPosition = sample.Position;
        Rotation = new Vector3(0f, Mathf.DegToRad(sample.Yaw), 0f);
        UpdateHeight(sample.IsCrouching);
    }

    private void UpdateHeight(bool crouching)
    {
        if (_body?.Mesh is not CapsuleMesh capsule)
        {
            return;
        }

        float height = crouching ? HitScan.CrouchHeight : HitScan.StandingHeight;
        capsule.Height = height;
        _body.Position = new Vector3(0f, height / 2f, 0f);
    }
}
