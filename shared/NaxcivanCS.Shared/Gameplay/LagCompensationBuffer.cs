// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Numerics;
using NaxcivanCS.Shared.Constants;

namespace NaxcivanCS.Shared.Gameplay;

/// <summary>Bir oyunçunun keçmiş mövqeyinin tək qeydi.</summary>
public readonly record struct PositionRecord(double ServerTimeMs, Vector3 Position, float Yaw, bool IsCrouching);

/// <summary>
/// PRD 45 - Lag compensation.
///
/// Server hər oyunçunun son ~200 ms mövqe tarixçəsini saxlayır. Atəş event-i
/// gəldikdə hədəflər atıcının latency-si qədər geriyə "sarılır" — beləliklə
/// oyunçu öz ekranında gördüyü mövqeyə atəş açır (PRD 46).
/// </summary>
public sealed class LagCompensationBuffer
{
    private readonly Queue<PositionRecord> _history = new();
    private readonly double _windowMs;

    public LagCompensationBuffer(double windowMs = GameConstants.LagCompensationHistoryMs)
    {
        if (windowMs <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(windowMs), "Pəncərə müsbət olmalıdır.");
        }

        _windowMs = windowMs;
    }

    public int Count => _history.Count;

    /// <summary>Ən köhnə qeyd ilə ən yeni qeyd arasındakı fərq, ms.</summary>
    public double SpanMs => _history.Count < 2
        ? 0
        : _history.Last().ServerTimeMs - _history.Peek().ServerTimeMs;

    /// <summary>Hər tick-də çağırılır; pəncərədən kənara düşən qeydləri atır.</summary>
    public void Record(double serverTimeMs, Vector3 position, float yaw, bool isCrouching)
    {
        _history.Enqueue(new PositionRecord(serverTimeMs, position, yaw, isCrouching));

        while (_history.Count > 1 && serverTimeMs - _history.Peek().ServerTimeMs > _windowMs)
        {
            _history.Dequeue();
        }
    }

    /// <summary>
    /// Verilmiş server vaxtındakı mövqeyi qaytarır — iki qeyd arasında xətti interpolyasiya ilə.
    /// Tarixçə boşdursa <c>null</c>, pəncərədən kənardırsa ən yaxın sərhəd qeydi qaytarılır.
    /// </summary>
    public PositionRecord? Rewind(double targetServerTimeMs)
    {
        if (_history.Count == 0)
        {
            return null;
        }

        PositionRecord[] records = _history.ToArray();

        if (targetServerTimeMs <= records[0].ServerTimeMs)
        {
            return records[0];
        }

        if (targetServerTimeMs >= records[^1].ServerTimeMs)
        {
            return records[^1];
        }

        for (int i = 0; i < records.Length - 1; i++)
        {
            PositionRecord older = records[i];
            PositionRecord newer = records[i + 1];

            if (targetServerTimeMs < older.ServerTimeMs || targetServerTimeMs > newer.ServerTimeMs)
            {
                continue;
            }

            double span = newer.ServerTimeMs - older.ServerTimeMs;
            float t = span <= 0 ? 0f : (float)((targetServerTimeMs - older.ServerTimeMs) / span);

            return new PositionRecord(
                targetServerTimeMs,
                Vector3.Lerp(older.Position, newer.Position, t),
                float.Lerp(older.Yaw, newer.Yaw, t),
                t < 0.5f ? older.IsCrouching : newer.IsCrouching);
        }

        return records[^1];
    }

    /// <summary>
    /// PRD 45, 47 - Atıcının bildirdiyi vaxta görə rewind hədəfi.
    /// Latency sui-istifadə edilə bilməsin deyə pəncərə ilə klamp edilir.
    /// </summary>
    public static double ResolveRewindTime(double serverTimeMs, double shooterLatencyMs,
        double maxRewindMs = GameConstants.LagCompensationHistoryMs)
    {
        double clamped = Math.Clamp(shooterLatencyMs, 0, maxRewindMs);
        return serverTimeMs - clamped;
    }

    public void Clear() => _history.Clear();
}
