# Silahlar, ammo və recoil

## Oynanıla bilən silah və kataloq

Server 8 JSON faylını yükləyir, amma hazırkı oyun hamıya **weapon_rifle_01 — AR-9 Qartal** verir. Kataloqda silahın olması onun oyun içində alınıb dəyişdirilə bilməsi demək deyil. Bıçaq melee, sniper scope və shotgun pellet loop ayrıca tamamlanmayıb.

| ID | Ad | Damage | RPM | Ammo | Reload, s | Qiymət |
|---|---|---:|---:|---:|---:|---:|
| weapon_knife_01 | Standart Bıçaq | 42 | 120 | 0/0 | 0 | 0 |
| weapon_pistol_01 | PS-1 Naxçıvan | 26 | 400 | 20/60 | 1.8 | 0 |
| weapon_pistol_02 | HP-5 Qaya | 38 | 250 | 7/35 | 2.2 | 700 |
| weapon_rifle_01 | AR-9 Qartal | 34 | 600 | 30/90 | 2.4 | 2700 |
| weapon_rifle_02 | DR-7 Sərhəd | 36 | 545 | 25/75 | 2.8 | 3100 |
| weapon_shotgun_01 | SG-2 Ordubad | 23 | 68 | 8/32 | 0.55 | 1900 |
| weapon_smg_01 | SM-3 Duzdağ | 27 | 850 | 30/120 | 2.1 | 1250 |
| weapon_sniper_01 | SN-8 Batabat | 115 | 41 | 10/30 | 3.7 | 4750 |

Cədvəldə damage zirehsiz chest baza zərəridir. RPM konfiqurasiya dəyəridir; real kadensiya 64 Hz tick diskretləşməsindən təsirlənir. 600 RPM üçün 0.1 saniyə interval 7 tick-ə yuvarlaqlaşa bilər; dəqiq 600 RPM ölçümü kimi göstərilməməlidir.

## Server weapon lifecycle

WeaponRuntime hər oyunçu üçün Weapon, AmmoInMagazine, ReserveAmmo, IsReloading, ShotIndex, Punch və LastShotPunch saxlayır. Tick-in ardıcıllığı:

1. Son atışdan keçən vaxt artırılır.
2. Reload davam edirsə yalnız reload taymeri işləyir.
3. R basılıbsa və ya şarjor boşdursa uyğun reload başlanır.
4. Tətik vəziyyəti automatic/semi-automatic qaydasına görə qiymətləndirilir.
5. Ammo və shot interval uyğundursa bir güllə çıxır.
6. Güllə cari recoil ilə gedir, növbəti recoil sonra hesablanır.

Bu ayırma client input tezliyini silahın atəş tezliyindən ayırır. Bir tick-də çox input gəlsə də WeaponRuntime bir dəfə çağırılır.

## Reload qaydaları

Dolu şarjor və ya reserve=0 olduqda reload başlanmır. Bitəndə needed=magazineSize−currentMagazine hesablanır, min(needed,reserve) qədər güllə köçürülür. Reserve mənfi olmur. Respawn silahın default ammo-sunu bərpa edir.

Semi-automatic silah üçün trigger kənarı istifadə edilir: davamlı basılılıq yeni atış yaratmır. Bu runtime davranışı test olunur, lakin client weapon selection hələ yoxdur.

## Recoil

Hər JSON-da vertical/horizontal addımlar siyahısı var. RecoilCalculator hər addımı 0.65 dərəcə/unit miqyasında toplayır; pattern bitəndə son addım təkrarlanır.

| Parametr | Dəyər |
|---|---:|
| Recovery gecikməsi | 0.12 s |
| Recovery sürəti | 22 dərəcə/s |
| Spray index reset | 0.35 s |
| İlk güllə Punch | 0 |

Server istiqamətə LastShotPunch tətbiq edir. Client ShotFired-dan sonra kamera/view model kick-i göstərir. Client vizual kick-i dəyişdirsə belə server recoil vəziyyəti saxlanılır; bu, bütün aim hiylələrinin həll edildiyi anlamına gəlmir.

## Konfiqurasiya və tətbiq sərhədi

Damage, armorPenetration, fireRate, magazine/reserve, reloadTime, range/falloff və recoil aktiv tüfəng üçün işləyir. Price/KillReward economy-yə, MovementSpeed movement-ə, Spread sahələri random trajectory-yə tam bağlanmayıb. Hot reload yoxdur; dəyişiklikdən sonra serveri yenidən başladın.

Testlər cadence, ilk güllə, reload zamanı atəşin bloklanması, avtomatik reload, ammo tükənməsi, semi-auto və codec round-trip ssenarilərini əhatə edir.

[Mənbə: WeaponRuntime](../../shared/NaxcivanCS.Shared/Gameplay/WeaponRuntime.cs) · [Konfiqurasiya](Configuration.md) · [Damage](Damage-and-Armor.md)
