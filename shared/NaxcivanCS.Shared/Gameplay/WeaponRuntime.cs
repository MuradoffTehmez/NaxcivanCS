// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later

using NaxcivanCS.Shared.Models;

namespace NaxcivanCS.Shared.Gameplay;

/// <summary>Bir tick-də silahın nə etməli olduğu.</summary>
public enum WeaponAction
{
    None = 0,
    Fire = 1,
    ReloadStarted = 2,
    ReloadFinished = 3,
    DryFire = 4,
}

/// <summary>
/// PRD 14, 17, 47, 156 - Bir oyunçunun silahının server tərəfdəki vəziyyəti.
///
/// <para>
/// <b>Atəş kadensiyasını server təyin edir.</b> Client yalnız "tetik basılıdır"
/// deyir; neçə güllə çıxacağını bu sinif qərara alır. Beləliklə fire-rate
/// hiyləsi <i>aşkarlanmır — mümkün olmur</i>.
/// </para>
///
/// <para>
/// Əvvəlki dizayn hər input paketini ayrıca atış cəhdi sayırdı. 64 tick/saniyə
/// input × 600 RPM (10 atış/saniyə) silah = saniyədə ~54 "mümkün olmayan atəş
/// sürəti" pozuntusu. Qanuni oyunçu bir saniyədən az müddətdə anti-cheat
/// həddini keçirdi (PRD 48). İndi belə bir vəziyyət yarana bilmir.
/// </para>
/// </summary>
public sealed class WeaponRuntime
{
    private float _secondsSinceLastShot = float.MaxValue;
    private float _reloadRemaining;
    private bool _fireHeldPreviousTick;

    public WeaponRuntime(WeaponData weapon)
    {
        ArgumentNullException.ThrowIfNull(weapon);

        Weapon = weapon;
        AmmoInMagazine = weapon.MagazineSize;
        ReserveAmmo = weapon.ReserveAmmo;
    }

    public WeaponData Weapon { get; private set; }

    public int AmmoInMagazine { get; private set; }

    public int ReserveAmmo { get; private set; }

    public bool IsReloading => _reloadRemaining > 0f;

    /// <summary>Cari spray-də neçənci atış (0-dan). Recoil pattern bunu istifadə edir.</summary>
    public int ShotIndex { get; private set; }

    /// <summary>
    /// PRD 17 - NÖVBƏTİ güllənin daşıyacağı recoil sapması.
    /// Atəşdən sonra artır, ona görə kameranın kick-i üçün də bu istifadə olunur.
    /// </summary>
    public RecoilPunch Punch { get; private set; } = RecoilPunch.Zero;

    /// <summary>
    /// PRD 17, 18 - Sonuncu gülləyə TƏTBİQ OLUNMUŞ sapma.
    ///
    /// <para>
    /// Spray-in <b>ilk gülləsi tam dəqiqdir</b> (sapma sıfır) — bu, taktiki
    /// FPS-in təməl qaydasıdır: dayanıb bir dəfə atəş açan oyunçu crosshair-in
    /// tam getdiyi yerə vurmalıdır. Recoil yalnız <i>sonrakı</i> güllələrə təsir edir.
    /// </para>
    /// </summary>
    public RecoilPunch LastShotPunch { get; private set; } = RecoilPunch.Zero;

    public void Equip(WeaponData weapon)
    {
        ArgumentNullException.ThrowIfNull(weapon);

        Weapon = weapon;
        AmmoInMagazine = weapon.MagazineSize;
        ReserveAmmo = weapon.ReserveAmmo;
        _reloadRemaining = 0f;
        ResetSpray();
    }

    /// <summary>
    /// Bir server tick-i. Nə baş verdiyini qaytarır.
    /// </summary>
    /// <param name="fireHeld">Tetik basılıdırmı (client input-undan).</param>
    /// <param name="reloadRequested">Reload düyməsi basılıbmı.</param>
    /// <param name="delta">Tick müddəti, saniyə.</param>
    public WeaponAction Tick(bool fireHeld, bool reloadRequested, float delta)
    {
        _secondsSinceLastShot = MathF.Min(_secondsSinceLastShot + delta, float.MaxValue / 2f);

        // 1) Reload gedirsə — atəş YOXDUR. Bu yoxlama açıq olmalıdır:
        //    əvvəl reload metodu "hələ davam edir" üçün None qaytarırdı və
        //    kod atəş məntiqinə düşürdü, yəni oyunçu reload zamanı atəş açırdı.
        if (IsReloading)
        {
            _fireHeldPreviousTick = fireHeld;
            Punch = RecoilCalculator.Recover(Punch, _secondsSinceLastShot, delta);
            return AdvanceReload(delta);
        }

        // 2) Reload başlamalıdırmı? (əl ilə, və ya şarjor boşdursa avtomatik)
        if ((reloadRequested || AmmoInMagazine == 0) && TryStartReload())
        {
            _fireHeldPreviousTick = fireHeld;
            return WeaponAction.ReloadStarted;
        }

        // PRD 17 - Atəş dayandıqda recoil tədricən sönür.
        if (!fireHeld || AmmoInMagazine == 0)
        {
            Punch = RecoilCalculator.Recover(Punch, _secondsSinceLastShot, delta);
        }

        if (RecoilCalculator.ShouldResetSpray(_secondsSinceLastShot))
        {
            ShotIndex = 0;
        }

        bool triggerPulled = Weapon.Automatic ? fireHeld : fireHeld && !_fireHeldPreviousTick;
        _fireHeldPreviousTick = fireHeld;

        if (!triggerPulled)
        {
            return WeaponAction.None;
        }

        if (AmmoInMagazine <= 0)
        {
            // Boş şarjor: yalnız tetik yeni basıldıqda "klik" səsi.
            return Weapon.Automatic && _secondsSinceLastShot < Weapon.ShotInterval
                ? WeaponAction.None
                : WeaponAction.DryFire;
        }

        // Kadensiya yoxlaması — burada rədd etmək pozuntu DEYİL, sadəcə
        // silah hələ hazır deyil (PRD 14).
        if (_secondsSinceLastShot < Weapon.ShotInterval)
        {
            return WeaponAction.None;
        }

        AmmoInMagazine--;

        // Güllə CARİ sapma ilə gedir (ilk atışda sıfır), sapma ondan SONRA artır.
        LastShotPunch = Punch;
        Punch = RecoilCalculator.Advance(Punch, Weapon, ShotIndex);
        ShotIndex++;
        _secondsSinceLastShot = 0f;

        return WeaponAction.Fire;
    }

    /// <summary>PRD 14 - Reload. Şarjor doludursa və ya ehtiyat yoxdursa başlamır.</summary>
    public bool TryStartReload()
    {
        if (IsReloading || AmmoInMagazine >= Weapon.MagazineSize || ReserveAmmo <= 0)
        {
            return false;
        }

        _reloadRemaining = Weapon.ReloadTime;
        ResetSpray();
        return true;
    }

    public void RefillOnRespawn()
    {
        AmmoInMagazine = Weapon.MagazineSize;
        ReserveAmmo = Weapon.ReserveAmmo;
        _reloadRemaining = 0f;
        ResetSpray();
    }

    /// <summary>Davam edən reload-u irəli aparır; bitdikdə şarjoru doldurur.</summary>
    private WeaponAction AdvanceReload(float delta)
    {
        _reloadRemaining -= delta;
        if (_reloadRemaining > 0f)
        {
            return WeaponAction.None;
        }

        _reloadRemaining = 0f;

        int needed = Weapon.MagazineSize - AmmoInMagazine;
        int loaded = Math.Min(needed, ReserveAmmo);
        AmmoInMagazine += loaded;
        ReserveAmmo -= loaded;

        return WeaponAction.ReloadFinished;
    }

    private void ResetSpray()
    {
        ShotIndex = 0;
        Punch = RecoilPunch.Zero;
        LastShotPunch = RecoilPunch.Zero;
        _secondsSinceLastShot = float.MaxValue / 4f;
    }
}
