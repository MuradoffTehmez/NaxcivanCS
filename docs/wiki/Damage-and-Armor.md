# Damage, armor və hit qeydiyyatı

## Serverin atəşi qiymətləndirməsi

GameWorld recoil əlavə edilmiş aim istiqamətini HitValidator-a ötürür. Atəş başlanğıcı atıcının mövqeyi və göz hündürlüyüdür. Hər canlı, atıcıdan fərqli oyunçu namizəd sayılır. Hədəflərin tarixçəsi atıcının təxmini bir istiqamətli latency-si qədər rewind edilir və ən yaxın hitbox seçilir.

Bu build-də namizəd filtiri komanda fərqini yoxlamır və divar raycast-ı yoxdur. Buna görə damage texniki prototipdir, tam competitive hit registration deyil.

## Baza hesabı

```text
rawDamage = weapon.Damage × hitboxMultiplier
distanceDamage = rawDamage × falloffMultiplier
healthDamage = distanceDamage − armorReduction
```

| Hitbox | Multiplier |
|---|---:|
| Head | 4.0 |
| Chest | 1.0 |
| Stomach | 1.2 |
| Arms | 1.0 |
| Legs | 0.75 |

DamageRules bu hitbox enum-larını tanıyır; faktiki HitScan hansı hissəni müəyyən edə bilirsə həmin nəticə istifadə olunur. Bütün bədən hissələrinin ayrıca detal mesh/hitbox kimi modelləşdiyi iddia edilmir.

## Məsafəyə görə azalma

Range daxilində falloff=1-dir. Range-dən sonra `clamp(1 − excessDistance × DamageFalloff, 0.1, 1)` işləyir. Məsələn AR-9 üçün range=32 və falloff=0.012 olduqda 42 vahiddə multiplier 0.88-dir.

## Armor

Armor varsa və hit head deyilsə, yaxud VestAndHelmet varsa, azalma tətbiq edilir:

```text
reductionRatio = (1 − ArmorPenetration) × 0.5
absorbed = distanceDamage × reductionRatio
armorDamage = min(targetArmor, round(absorbed))
healthDamage = round(distanceDamage − absorbed)
```

AR-9 yaxın məsafədə zirehsiz chest üçün 34, zirehsiz head üçün 136 verir. ArmorPenetration=0.7 ilə uyğun armor olduqda chest health damage təxminən 29 olur.

Cari formul azalmadan əvvəl mövcud armor-un çatıb-çatmadığına görə absorbed dəyərini yenidən miqyaslamır; bu model balans qərarı kimi ayrıca nəzərdən keçirilməlidir. Spawn/respawn armor 0 olduğu üçün armor alış axını olmadan normal prototipdə zirehsiz başlanır.

## Hadisələr və UI

Damage reliable mesajdır. Lethal nəticə PlayerKilled növü ilə göndərilir; server health/death/kill statistikasını dəyişir. Shooter HUD hitmarker-i bu server hadisəsinə əsaslanır. ShotFired ayrıca vizual hadisədir və unreliable gedir.

[Mənbə: DamageRules](../../shared/NaxcivanCS.Shared/Constants/DamageRules.cs) · [Lag compensation](Netcode.md) · [Məhdudiyyətlər](../KNOWN_ISSUES.md)
