using System.Numerics;

namespace NaxcivanCS.Shared.Gameplay;

/// <summary>
/// PRD 83 - Addım səslərinin nə vaxt səslənəcəyini təyin edir.
///
/// <para>
/// Addımlar taymerlə deyil, <b>qət edilmiş məsafə</b> ilə ölçülür — beləliklə
/// sürət dəyişəndə addımlar avtomatik uyğunlaşır və interpolyasiya olunmuş
/// uzaq oyunçular üçün də düzgün işləyir.
/// </para>
///
/// <para>
/// <b>Rəqabət üçün kritik:</b> Shift ilə addımlayan və çömbəlmiş oyunçu səssiz
/// olmalıdır (PRD 12, 83). Bu, taktiki FPS-in təməl mexanikasıdır — səssiz
/// yaxınlaşma imkanı olmasa, xəritə biliyi və pozisiya mənasını itirir.
/// Səs mənbəyi server-authoritative mövqelərdir, ona görə oyunçu öz addım
/// səsini başqasının client-ində gizlədə bilmir.
/// </para>
/// </summary>
public sealed class FootstepTracker
{
    /// <summary>İki addım arasındakı məsafə, metr.</summary>
    public const float StrideLength = 1.9f;

    /// <summary>
    /// Bu sürətdən aşağıda addım səsi çıxmır.
    /// <see cref="MovementSimulation.WalkSpeed"/>-dən bir qədər yuxarıdır ki,
    /// Shift ilə addımlamaq etibarlı şəkildə səssiz olsun.
    /// </summary>
    public static readonly float SilentSpeedThreshold = MovementSimulation.WalkSpeed + 0.35f;

    private Vector3 _lastPosition;
    private float _accumulatedDistance;
    private bool _hasPosition;

    /// <summary>Son hesablanmış üfüqi sürət, m/s.</summary>
    public float CurrentSpeed { get; private set; }

    /// <summary>
    /// Yeni mövqe ilə yenilənir və addım səsi çıxmalıdırsa <c>true</c> qaytarır.
    /// </summary>
    /// <param name="position">Oyunçunun cari mövqeyi.</param>
    /// <param name="deltaSeconds">Ötən vaxt.</param>
    /// <param name="isGrounded">Yerdədirmi — havada addım səsi olmur.</param>
    /// <param name="isCrouching">Çömbəlibsə səssizdir.</param>
    public bool Update(Vector3 position, float deltaSeconds, bool isGrounded, bool isCrouching)
    {
        if (!_hasPosition)
        {
            _hasPosition = true;
            _lastPosition = position;
            return false;
        }

        Vector3 delta = position - _lastPosition;
        _lastPosition = position;

        // Yalnız üfüqi hərəkət sayılır — düşmək və ya qalxmaq addım deyil.
        var horizontal = new Vector2(delta.X, delta.Z);
        float distance = horizontal.Length();

        CurrentSpeed = deltaSeconds > 0f ? distance / deltaSeconds : 0f;

        if (!isGrounded || isCrouching || CurrentSpeed < SilentSpeedThreshold)
        {
            // Səssiz hərəkətdə məsafə yığılmır: oyunçu addımlayaraq yaxınlaşıb
            // sonra qaçmağa başlayanda dərhal addım səsi çıxmamalıdır.
            _accumulatedDistance = 0f;
            return false;
        }

        _accumulatedDistance += distance;

        if (_accumulatedDistance < StrideLength)
        {
            return false;
        }

        _accumulatedDistance -= StrideLength;
        return true;
    }

    /// <summary>Respawn və ya teleportdan sonra çağırılır.</summary>
    public void Reset()
    {
        _hasPosition = false;
        _accumulatedDistance = 0f;
        CurrentSpeed = 0f;
    }
}
