# Hərəkət, kamera və nişan

## Hərəkət modeli

MovementSimulation input-dan sürəti hesablayır. Eyni shared kod client prediction və server authority üçün istifadə edilir. Dünya ilə kolliziyanı Godot CharacterBody3D.MoveAndSlide həll edir; shared simulyasiya təkbaşına divarla toqquşmanı hesablamır.

| Parametr | Kod dəyəri |
|---|---:|
| Qaçış sürəti | 7.6 |
| Addımlama sürəti | 4.1 |
| Çömbəlmə sürəti | 3.2 |
| GroundAcceleration | 62 |
| GroundFriction | 48 |
| AirAcceleration | 12 |
| AirControl | 0.28 |
| JumpVelocity | 5.4 |
| Gravity | 18.6 |
| Pitch sərhədi | ±89° |

Bunlar engine vahidləri ilə ilkin prototip sabitləridir. WeaponData.MovementSpeed sahəsi mövcud olsa da silah başına movement multiplier bu axına qoşulmayıb.

## Kamera

Default FOV 90°-dir; shared-də 75–110 sərhədləri var, amma istifadəçi üçün tam settings ekranı yoxdur. Mouse sensitivity lokal controller-də 0.12 sabitidir. Ctrl yerdə çömbəlmə vəziyyətini dəyişir; kamera hündürlüyü yumşaq interpolate olunur.

Godot istiqaməti -Z irəlidir. Alpha spawn baxışı 180°, Bravo 0°-dir. Bu fərq hər iki komandanın mərkəzə baxmasını təmin edir.

## Lokal cavab və server düzəlişi

Hərəkət mouse/klaviaturaya gecikmədən vizual cavab vermək üçün əvvəl client-də proqnozlaşdırılır. Server snapshot-ı gəldikdə mövqe fərqi 0.08 vahiddən böyükdürsə reconciliation olunur. Divar yanında və zəif şəbəkədə görünən kiçik düzəliş bu mexanizmlə əlaqəli ola bilər.

Tam state reconciliation hələ tamam deyil: snapshot velocity və grounded state daşımır. Bu səbəbdən mövqe düzəlişi bütün fizika vəziyyətinin tam əvəzlənməsi deyil.

## Accuracy ilə əlaqə

Shared qaydalarında stance multiplier-ləri var: crouch 0.7, standing 1, walking 1.6, running 3, airborne 8. Bunlar DamageRules.CalculateSpread daxilində hesablanır, amma canlı hitscan istiqamətinə random spread kimi tətbiq olunmur. Shift-i “hazırda səssiz və daha dəqiq atəş rejimi” kimi təqdim etmək düzgün deyil; footstep sistemi də yoxdur.

LandingSlowdown=0.72 sabiti mövcuddur, lakin cari state keçidi onu düzgün tetikləmir. Bu, məlum movement borcudur.

## Proqramçı üçün test

Diaqonal hərəkət, acceleration/friction, jump, havada idarə, pitch clamp və reconciliation threshold shared testlərdə yoxlanır. Engine kolliziyası, stair/crouch keçidləri və packet-loss altında düzəliş üçün ayrıca integration/manual test lazımdır.

[Mənbə](../../shared/NaxcivanCS.Shared/Gameplay/MovementSimulation.cs) · [Netcode](Netcode.md)
