using System.Numerics;
using NaxcivanCS.Shared.Enums;
using NaxcivanCS.Shared.Models;
using NaxcivanCS.Shared.Net;

namespace NaxcivanCS.Shared.Gameplay;

/// <summary>
/// PRD 11, 12, 43 - Hərəkət simulyasiyası.
///
/// Bu sinif <b>eyni kod</b> kimi iki yerdə işləyir: client-də prediction üçün,
/// serverdə isə authoritative nəticə üçün (PRD 39). Deterministik olması üçün
/// Godot-a və ya real vaxta istinad etmir — yalnız giriş vəziyyəti + input + sabit delta.
///
/// <para>
/// Qeyd: kolliziya həlli mühərrikə (Godot <c>MoveAndSlide</c>) buraxılıb.
/// Bu sinif sürəti hesablayır, mühərrik isə divarları tətbiq edir.
/// Tam deterministik kolliziya Phase 2-də shared-ə köçürüləcək (PRD 43).
/// </para>
/// </summary>
public static class MovementSimulation
{
    // PRD 12 - Hərəkət parametrləri. MVP default-ları; balans mərhələsində config-ə köçür.
    public const float RunSpeed = 7.6f;
    public const float WalkSpeed = 4.1f;
    public const float CrouchSpeed = 3.2f;
    public const float GroundAcceleration = 62f;
    public const float GroundFriction = 48f;
    public const float AirAcceleration = 12f;
    public const float AirControl = 0.28f;
    public const float JumpVelocity = 5.4f;
    public const float Gravity = 18.6f;
    public const float MaxPitchDegrees = 89f;

    /// <summary>Yerə enəndə tətbiq olunan sürət cəriməsi (PRD 12 - landing slowdown).</summary>
    public const float LandingSlowdown = 0.72f;

    /// <summary>
    /// PRD 43 - Reconciliation həddi: server mövqeyi client proqnozundan
    /// bu qədər çox fərqlənirsə, client düzəliş etməlidir.
    /// </summary>
    public const float ReconciliationThreshold = 0.08f;

    /// <summary>PRD 47 - Serverin movement validasiyası üçün maksimum icazəli sürət.</summary>
    public static float MaxPossibleSpeed => RunSpeed;

    /// <summary>Bir tick üçün hərəkəti hesablayır və yeni vəziyyəti qaytarır.</summary>
    /// <param name="state">Cari vəziyyət.</param>
    /// <param name="input">Client-dən gələn input (PRD 46 - yeganə gameplay girişi).</param>
    /// <param name="delta">Sabit tick addımı, saniyə.</param>
    public static MovementState Step(MovementState state, InputCommand input, float delta)
    {
        bool wasAirborne = !state.IsGrounded;

        state.Yaw = NormalizeDegrees(input.YawDegrees);
        state.Pitch = Math.Clamp(input.PitchDegrees, -MaxPitchDegrees, MaxPitchDegrees);
        state.IsCrouching = input.Buttons.HasFlag(InputButtons.Crouch) && state.IsGrounded;

        Vector3 wishDirection = WishDirection(input, state.Yaw);
        float targetSpeed = TargetSpeed(input, state.IsCrouching);

        var horizontal = new Vector3(state.Velocity.X, 0f, state.Velocity.Z);

        if (state.IsGrounded)
        {
            if (wasAirborne)
            {
                horizontal *= LandingSlowdown;
            }

            horizontal = ApplyFriction(horizontal, delta);
            horizontal = Accelerate(horizontal, wishDirection, targetSpeed, GroundAcceleration, delta);
        }
        else
        {
            horizontal = Accelerate(horizontal, wishDirection, targetSpeed * AirControl, AirAcceleration, delta);
        }

        float verticalVelocity = state.Velocity.Y;

        if (state.IsGrounded && input.Buttons.HasFlag(InputButtons.Jump))
        {
            verticalVelocity = JumpVelocity;
            state.IsGrounded = false;
        }
        else if (!state.IsGrounded)
        {
            verticalVelocity -= Gravity * delta;
        }
        else if (verticalVelocity < 0f)
        {
            verticalVelocity = 0f;
        }

        state.Velocity = new Vector3(horizontal.X, verticalVelocity, horizontal.Z);
        state.LastProcessedSequence = input.Sequence;
        return state;
    }

    /// <summary>PRD 18 - Cari sürətə görə stance (accuracy cəriməsi üçün).</summary>
    public static Stance ResolveStance(in MovementState state, bool walkHeld)
    {
        if (!state.IsGrounded)
        {
            return Stance.Airborne;
        }

        if (state.IsCrouching)
        {
            return Stance.Crouching;
        }

        float speed = HorizontalSpeed(state);
        if (speed < 0.25f)
        {
            return Stance.Standing;
        }

        return walkHeld || speed <= WalkSpeed + 0.1f ? Stance.Walking : Stance.Running;
    }

    /// <summary>Maksimum sürətə nisbət, 0..1 — spread hesablamasında istifadə olunur.</summary>
    public static float VelocityRatio(in MovementState state)
        => Math.Clamp(HorizontalSpeed(state) / RunSpeed, 0f, 1f);

    public static float HorizontalSpeed(in MovementState state)
        => new Vector2(state.Velocity.X, state.Velocity.Z).Length();

    public static bool NeedsReconciliation(Vector3 predicted, Vector3 authoritative)
        => Vector3.Distance(predicted, authoritative) > ReconciliationThreshold;

    private static Vector3 WishDirection(InputCommand input, float yawDegrees)
    {
        float yawRadians = yawDegrees * MathF.PI / 180f;
        float sin = MathF.Sin(yawRadians);
        float cos = MathF.Cos(yawRadians);

        // Godot konvensiyası: -Z irəli.
        var forward = new Vector3(-sin, 0f, -cos);
        var right = new Vector3(cos, 0f, -sin);

        Vector3 wish = (forward * input.MoveForward) + (right * input.MoveRight);
        return wish.LengthSquared() > 1e-6f ? Vector3.Normalize(wish) : Vector3.Zero;
    }

    private static float TargetSpeed(InputCommand input, bool isCrouching)
    {
        if (isCrouching)
        {
            return CrouchSpeed;
        }

        return input.Buttons.HasFlag(InputButtons.Walk) ? WalkSpeed : RunSpeed;
    }

    private static Vector3 ApplyFriction(Vector3 horizontal, float delta)
    {
        float speed = horizontal.Length();
        if (speed < 1e-4f)
        {
            return Vector3.Zero;
        }

        float drop = GroundFriction * delta;
        float newSpeed = MathF.Max(0f, speed - drop);
        return horizontal * (newSpeed / speed);
    }

    private static Vector3 Accelerate(Vector3 horizontal, Vector3 wishDirection, float targetSpeed, float acceleration, float delta)
    {
        if (wishDirection == Vector3.Zero)
        {
            return horizontal;
        }

        float currentSpeed = Vector3.Dot(horizontal, wishDirection);
        float addSpeed = targetSpeed - currentSpeed;
        if (addSpeed <= 0f)
        {
            return horizontal;
        }

        float accelerationSpeed = MathF.Min(acceleration * delta * targetSpeed / RunSpeed, addSpeed);
        return horizontal + (wishDirection * accelerationSpeed);
    }

    private static float NormalizeDegrees(float degrees)
    {
        degrees %= 360f;
        return degrees < 0f ? degrees + 360f : degrees;
    }
}
