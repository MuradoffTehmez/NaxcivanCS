# Layihə analizi — 0.2.2

> Bu sənəd 0.2.2 tarixi snapshot-ıdır. Cari stable və unreleased vəziyyət üçün [README](../README.md) və [CHANGELOG](../CHANGELOG.md) əsasdır.

## İcra xülasəsi

NaxcivanCS işlək multiplayer prototip bazasına malikdir. Shared domain ayrılması, binar protokol, server authority, iki client smoke testi və yeni WeaponRuntime yaxşı inkişaf dayaqlarıdır. Məhsul tam 5v5 Bomb/Defuse kimi təqdim edilməməlidir: əsas raund, objective, inventory və online xidmət axınları hələ inteqrasiya olunmayıb.

Analiz bazası: main-in v0.2.1 tarixi, feature/gunplay-feedback budağındakı 8def91b dəyişiklikləri və 0.2.2 sənədləşdirmə/versiya hazırlığı. PRD 160 bölməli geniş hədəf sənədidir; runtime hesabatı kimi oxunmamalıdır.

## Komponent qiymətləndirməsi

| Komponent | Güclü tərəf | Sərhəd |
|---|---|---|
| Shared | Godot-dan asılı deyil, xUnit ilə yoxlanır | Bəzi qaydalar runtime-a bağlanmayıb |
| Client | Input, prediction, kamera, HUD, procedural feedback | Tam menyu, audio, localization, settings yoxdur |
| Server | Dedicated model, weapon cadence və damage qərarı | Dünya occlusion, vaxt büdcəsi və lifecycle boşluqları |
| Backend | Sadə minimal API və versiya contract-ı | Yalnız yaddaş registry-si, account/auth yoxdur |
| Config | 8 silah üçün JSON mənbəyi | Server config loader/hot reload yoxdur |
| CI | Shared/backend, Godot builds, E2E, audit, backend Docker | Format informativdir; gameplay/render və yük testləri yoxdur |
| Sənədlər | Geniş məhsul düşüncəsi və mərhələlər | Əvvəlki statuslar planı və icranı qarışdırırdı |

## Gunplay budağının əsas dəyişməsi

Əvvəl hər input paketi ayrıca atəş cəhdi kimi qiymətləndirilirdi. Davamlı tətik qanuni oyunçu üçün belə fire-rate pozuntu balları yarada bilirdi. Yeni dizayn serverdə silahın vaxtını və ammo-sunu saxlayır, tick başına bir silah addımı icra edir. Hazır olmayan silah sadəcə atəş açmır. Reload davam edərkən atəş kəsilir; recoil yeni atışdan sonra növbəti gülləyə hazırlanır.

Bunun vizual tərəfi server ShotFired hadisəsindən başlayır. Client əvvəlcədən hit nəticəsi iddia etmir. Bu yanaşma düzgün authority sərhədi yaradır, amma effektin hiss edilən gecikməsi şəbəkədən asılı qalır.

## Buraxılışda görülən işlər

- Gunplay dəyişiklikləri buraxılış bazasına daxil edildi.
- Build/game/content 0.2.2 oldu; iki yeni server mesajı ilə protocol 2-yə keçirildi.
- README, arxitektura və roadmap faktiki tətbiqə uyğunlaşdırıldı.
- Oyunçu, proqramçı və operator üçün mövzu əsaslı Wiki yaradıldı.
- Töhfə, support, security, asset, test və release sənədləri əlavə edildi.
- Link yoxlaması və ayrıca GitHub Wiki reposuna nəşr aləti əlavə edildi.

## Prioritet təklifi

| Prioritet | İş | Qəbul meyarı |
|---|---|---|
| P1 | Dünya occlusion və team damage qaydası | Divar arxasında zərər yoxdur; friendly fire rejimi test olunur |
| P1 | Input/vaxt büdcəsi və giriş validasiyası | Çox paket simulyasiya vaxtını artırmır; malformed state yayılmır |
| P1 | Ammo və respawn sync | İlk spawn, respawn və packet loss-dan sonra HUD serverə uyğunlaşır |
| P2 | Tam movement reconciliation | Velocity/grounded keçidləri və landing davranışı sabitdir |
| P2 | Round → bomb → economy | İki komanda tam raundu başlanğıcdan sonadək oynaya bilir |
| P2 | Config loader və balans | JSON dəyişməsi müvafiq runtime nəticəsini dəyişir |
| P3 | Audio/localization/settings | Real input playtestdə oxunaqlı, idarə olunan UI |
| P3 | Backend persistence/auth | Etibarlı hesab və server qeydiyyatı, restart sonrası məlumat |

P1 burada məhsulun texniki prioritetidir; CVSS və ya formal təhlükəsizlik dərəcəsi deyil. Təfərrüatlar [KNOWN_ISSUES](KNOWN_ISSUES.md) sənədindədir.

## Yoxlama və sübut sərhədi

97 shared test ilkin analizdə keçdi; solution, client və server build-ləri 0 warning/0 error verdi. Release versiyası ilə yekun təkrar yoxlama [buraxılış hesabatında](RELEASE-0.2.2.md) saxlanır.

Kod baxışı static occlusion, server config istifadəsi və event sync kimi axın boşluqlarını müəyyən etdi. Geniş təhlükəsizlik auditi, 10 oyunçulu yük ölçümü, real gunplay balans testi və production deployment bu analizdə təsdiqlənməyib.

## Sənəd baxımı

Məhsul niyyəti PRD-də, icra prioriteti ROADMAP-də, real davranış Wiki və KNOWN_ISSUES-də qalır. Yeni funksiya üçün kodun mövcudluğu ilə yanaşı scene/lifecycle inteqrasiyası və qəbul testi də göstərilməlidir. Bu ayrım gələcək mərhələlərdə yanlış “hazırdır” siqnallarını azaldır.
