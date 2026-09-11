// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using Godot;
using NaxcivanCS.Shared.Enums;

namespace NaxcivanCS.Client.Audio;

/// <summary>
/// PRD 83, 84, 143, 144 - Oyun səslərinin kodda sintezi.
///
/// <para>
/// Bütün səslər burada riyazi olaraq yaradılır — layihədə heç bir audio faylı
/// yoxdur. Səbəbi hüquqidir: PRD 143 orijinal audio tələb edir, PRD 144 isə
/// başqa oyunların səslərini qadağan edir. Sintez edilmiş səs təbiətcə
/// orijinaldır və lisenziya riski daşımır.
/// </para>
///
/// <para>
/// Bu, son keyfiyyət deyil — real səs dizaynı Phase 2/3-də gələcək. Məqsəd
/// gunplay-in <b>hiss olunması</b> üçün lazım olan minimumdur (PRD 151).
/// </para>
/// </summary>
public static class ProceduralAudio
{
    private const int SampleRate = 44100;

    /// <summary>Sintez determinik olsun deyə sabit toxum.</summary>
    private const int Seed = 20260911;

    // ------------------------------------------------------------------ Silah

    /// <summary>
    /// Tüfəng atəşi: sürətli "crack" + aşağı tezlikli gövdə + qısa quyruq.
    /// </summary>
    public static AudioStreamWav RifleShot()
    {
        const float duration = 0.38f;
        int count = (int)(SampleRate * duration);
        var samples = new float[count];
        var rng = new Random(Seed);

        float lowpassState = 0f;
        float highpassState = 0f;
        float previousNoise = 0f;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);

            // Kəskin "crack" — differensiallaşdırılmış küy yüksək tezlik verir.
            float crack = (noise - previousNoise) * Decay(t, 0.006f);
            previousNoise = noise;

            // Gövdə — alçaq tezlikli küy, bir qədər uzun sönmə.
            lowpassState += (noise - lowpassState) * 0.12f;
            float body = lowpassState * Decay(t, 0.045f) * 1.6f;

            // Partlayışın aşağı "thump"-ı.
            float thump = MathF.Sin(2f * MathF.PI * 78f * t) * Decay(t, 0.07f) * 0.9f;

            // Quyruq — otağın əks-sədası əvəzinə sadə sönən küy.
            highpassState += (noise - highpassState) * 0.03f;
            float tail = (noise - highpassState) * Decay(t, 0.16f) * 0.18f;

            samples[i] = (crack * 1.4f) + body + thump + tail;
        }

        return Build(samples);
    }

    /// <summary>Boş şarjor: quru "klik".</summary>
    public static AudioStreamWav DryFire() => Click(0.045f, 2600f, 0.004f, 0.35f);

    /// <summary>Şarjorun çıxarılması — bir qədər dərin mexaniki klik.</summary>
    public static AudioStreamWav MagazineOut() => Click(0.09f, 900f, 0.012f, 0.5f);

    /// <summary>Şarjorun yerinə oturması — daha kəskin və ucadan.</summary>
    public static AudioStreamWav MagazineIn() => Click(0.11f, 1500f, 0.016f, 0.7f);

    // ----------------------------------------------------------------- Vurulma

    /// <summary>Güllənin divara/yerə dəyməsi.</summary>
    public static AudioStreamWav WorldImpact()
    {
        const float duration = 0.16f;
        int count = (int)(SampleRate * duration);
        var samples = new float[count];
        var rng = new Random(Seed + 1);

        float lowpass = 0f;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);

            lowpass += (noise - lowpass) * 0.35f;

            samples[i] = lowpass * Decay(t, 0.018f)
                + (MathF.Sin(2f * MathF.PI * 210f * t) * Decay(t, 0.03f) * 0.5f);
        }

        return Build(samples);
    }

    /// <summary>Oyunçuya dəymə — daha "yumşaq" və alçaq.</summary>
    public static AudioStreamWav FleshImpact()
    {
        const float duration = 0.2f;
        int count = (int)(SampleRate * duration);
        var samples = new float[count];
        var rng = new Random(Seed + 2);

        float lowpass = 0f;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);

            lowpass += (noise - lowpass) * 0.08f;

            samples[i] = (lowpass * Decay(t, 0.035f) * 1.4f)
                + (MathF.Sin(2f * MathF.PI * 120f * t) * Decay(t, 0.05f) * 0.6f);
        }

        return Build(samples);
    }

    /// <summary>
    /// PRD 78 - Hit marker. Yalnız atıcının öz client-ində, 2D səslənir.
    /// Qısa və yüksək — atəş səsinin içində eşidilməlidir.
    /// </summary>
    public static AudioStreamWav HitMarker(bool killed)
    {
        float duration = killed ? 0.14f : 0.06f;
        int count = (int)(SampleRate * duration);
        var samples = new float[count];

        float frequency = killed ? 880f : 1450f;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;

            // Kill zamanı iki tonlu qısa motiv — vurma ilə qarışmasın.
            float f = killed && t > 0.06f ? frequency * 1.5f : frequency;
            samples[i] = MathF.Sin(2f * MathF.PI * f * t) * Decay(t, 0.02f) * 0.55f;
        }

        return Build(samples);
    }

    // ---------------------------------------------------------------- Addımlar

    /// <summary>
    /// PRD 84 - Səthə görə addım səsi. Hər material fərqli tembr verir ki,
    /// oyunçu qulaqla "harada gəzir" sualına cavab verə bilsin.
    /// </summary>
    public static AudioStreamWav Footstep(SurfaceMaterial surface)
    {
        (float duration, float lowpassCoefficient, float decayTau, float ringHz, float ringLevel) = surface switch
        {
            //            müddət  filtr   sönmə   rezonans  səviyyə
            SurfaceMaterial.Stone => (0.13f, 0.45f, 0.016f, 0f, 0f),
            SurfaceMaterial.Concrete => (0.12f, 0.5f, 0.014f, 0f, 0f),
            SurfaceMaterial.Wood => (0.17f, 0.22f, 0.028f, 190f, 0.45f),
            SurfaceMaterial.Metal => (0.3f, 0.6f, 0.02f, 1180f, 0.5f),
            SurfaceMaterial.Glass => (0.22f, 0.75f, 0.012f, 2300f, 0.4f),
            SurfaceMaterial.Grass => (0.2f, 0.3f, 0.045f, 0f, 0f),
            SurfaceMaterial.Sand => (0.24f, 0.2f, 0.06f, 0f, 0f),
            SurfaceMaterial.Water => (0.28f, 0.16f, 0.05f, 320f, 0.3f),
            _ => (0.12f, 0.5f, 0.014f, 0f, 0f),
        };

        int count = (int)(SampleRate * duration);
        var samples = new float[count];
        var rng = new Random(Seed + (int)surface);

        float filterState = 0f;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);

            filterState += (noise - filterState) * lowpassCoefficient;
            float value = filterState * Decay(t, decayTau);

            if (ringHz > 0f)
            {
                value += MathF.Sin(2f * MathF.PI * ringHz * t) * Decay(t, decayTau * 2.2f) * ringLevel;
            }

            samples[i] = value;
        }

        // Yumşaq səthlər daha sakit olmalıdır — qumda qaçmaq daşdan sakitdir.
        float level = surface switch
        {
            SurfaceMaterial.Sand or SurfaceMaterial.Grass => 0.55f,
            SurfaceMaterial.Water => 0.75f,
            _ => 1f,
        };

        return Build(samples, level);
    }

    // ------------------------------------------------------------------ Köməkçi

    /// <summary>Eksponensial sönmə zərfi.</summary>
    private static float Decay(float t, float tau) => MathF.Exp(-t / tau);

    /// <summary>Qısa mexaniki klik: filtrlənmiş küy + rezonans.</summary>
    private static AudioStreamWav Click(float duration, float ringHz, float decayTau, float level)
    {
        int count = (int)(SampleRate * duration);
        var samples = new float[count];
        var rng = new Random(Seed + (int)(ringHz));

        float filterState = 0f;

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)SampleRate;
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);

            filterState += (noise - filterState) * 0.55f;

            samples[i] = (filterState * Decay(t, decayTau))
                + (MathF.Sin(2f * MathF.PI * ringHz * t) * Decay(t, decayTau * 1.5f) * 0.5f);
        }

        return Build(samples, level);
    }

    /// <summary>
    /// Float nümunələri normallaşdırıb 16-bit PCM axınına çevirir.
    /// Normallaşdırma klipinqin qarşısını alır — sintez zamanı laylar
    /// toplandığı üçün amplitud asanlıqla 1.0-ı keçir.
    /// </summary>
    private static AudioStreamWav Build(float[] samples, float level = 1f)
    {
        float peak = 0f;
        foreach (float sample in samples)
        {
            peak = MathF.Max(peak, MathF.Abs(sample));
        }

        float scale = peak > 0.0001f ? level * 0.92f / peak : 0f;

        var data = new byte[samples.Length * 2];
        for (int i = 0; i < samples.Length; i++)
        {
            var value = (short)Math.Clamp(samples[i] * scale * short.MaxValue, short.MinValue, short.MaxValue);
            data[i * 2] = (byte)(value & 0xFF);
            data[(i * 2) + 1] = (byte)((value >> 8) & 0xFF);
        }

        return new AudioStreamWav
        {
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = SampleRate,
            Stereo = false,
            Data = data,
        };
    }
}
