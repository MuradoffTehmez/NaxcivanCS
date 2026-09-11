# NaxcivanCS 0.2.2

Gunplay prototipi: server atəş kadensiyası, ammo/reload, deterministik recoil, prosedural silah modeli və vizual atəş feedback-i. Buraxılışa Azərbaycan dilində oyunçu/proqramçı/operator Wiki-si və geniş repo sənədləri daxildir.

## Uyğunluq

Git tag və game/content/build versiyası **0.2.2**, protocol **2**-dir. Protocol 1 client-ləri rədd edilir; client və server birlikdə yenilənməlidir.

## Mənbə

Gunplay mənbə commit-i: 8def91ba95e7dc86051d7872038449b8b401947a. Sənədləşdirmə və versiya hazırlığı bunun üzərinə əlavə edilir. Heç bir köhnə teq yenidən yazılmır.

## Yoxlama

2026-09-11, Windows, .NET SDK 10.0.401, Godot 4.7.2 .NET mühitində:

| Yoxlama | Nəticə |
|---|---|
| Solution Release build | Keçdi; 0 warning, 0 error |
| Shared xUnit | 97 keçdi, 0 uğursuz, 0 skipped |
| Godot server Debug build | Keçdi; 0 warning, 0 error |
| Godot client Debug build | Keçdi; 0 warning, 0 error |
| Headless E2E, protocol 2/game 0.2.2 | Keçdi; 2 client, 188 və 186 snapshot/6 s, 14 və 11 ms ping |
| Backend smoke | Health 200, version 0.2.2/protocol 2, heartbeat 202, list entry, blank serverId 400 |
| Markdown | 49 fayl, 217 lokal link; kod çəpərləri bağlı |
| Wiki export | 27 fayl: 25 əsas səhifə + sidebar/footer, təxminən 8500 söz |

Backend timeout-un 30 saniyədən sonra filtrasiyası ayrıca vaxtlı test edilməyib. Xarici HTTP linklər və heading anchor-ları link checker tərəfindən yoxlanmır. Lokal Docker/export, manual render/audio, 10 oyunçulu yük və uzunmüddətli soak sınağı aparılmayıb.

Gunplay mənbə budağının GitHub CI-si uğurludur. Buraxılış PR-si və main merge commit-i üçün aktual CI sübutu [GitHub Actions](https://github.com/MuradoffTehmez/NaxcivanCS/actions)-da saxlanır.

## Məhdudiyyətlər

Tam Bomb/Defuse, round/economy/inventory, online hesablar, matchmaking, audio və final xəritələr yoxdur. Divar occlusion və ammo sync daxil olmaqla konkret boşluqlar [KNOWN_ISSUES](KNOWN_ISSUES.md)-də göstərilir.

Hazır binary installer və production deployment bu buraxılışın deliverable-ı deyil. Repo mənbədən build və test prototipi təqdim edir.

[Changelog](../CHANGELOG.md) · [Wiki](wiki/Home.md) · [Analiz](PROJECT_ANALYSIS.md)
