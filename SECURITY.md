# Təhlükəsizlik

## Vəziyyət

Cari stable buraxılış araşdırma və lokal/LAN test prototipidir. Tam authentication, API rate limiting, heartbeat authorization və production moderasiya sistemi yoxdur. Server-authoritative dizayn bütün hücumlara qarşı zəmanət deyil.

## Dəstəklənən versiyalar

| Versiya | Dəstəklənir | Qeyd |
|---|---|---|
| 0.3.0 | :white_check_mark: | Cari stable buraxılış |
| < 0.3.0 | :x: | Köhnə prototiplər üçün dəstək verilmir |

Cari inkişaf bazası `main`-dir; buradakı unreleased kod stable buraxılış sayılmır. Əvvəlki teqlər üçün ayrıca təhlükəsizlik baxımı və cavab müddəti öhdəliyi elan edilməyib.

## Problem bildirmək

Məxfi zəiflik detalları, işlək exploit və tokenləri açıq issue/PR-də paylaşmayın. GitHub Security bölməsində private vulnerability reporting aktivdirsə həmin kanaldan istifadə edin. Aktiv deyilsə, əvvəlcə repo sahibindən məxfi kanal istəyən, texniki zəiflik təfərrüatı olmayan müraciət edin.

Hesabatda təsirlənən commit/teq, komponent, təsir, test mühiti, minimum reproduksiya və mümkün düzəliş olmalıdır. Şəxsi məlumatları və giriş sirlərini silin. İcazəsiz üçüncü tərəf serverlərini test etməyin.

## Avtomatik yoxlamalar

| Qat | Nə edir | Harada |
|---|---|---|
| **CodeQL** | C# və GitHub Actions üçün statik analiz (`security-extended`) | `codeql.yml`, həftəlik + hər PR |
| **Dependency review** | PR-ın gətirdiyi yeni asılılıqlarda orta+ zəiflik PR-ı bloklayır | `security.yml` |
| **Vulnerable package scan** | Transitive daxil məlum CVE-lər | `security.yml`, həftəlik |
| **Repository hygiene** | Gizli fayllar (`.env`, `.pem`, `.pfx`), 5 MB-dan böyük binarlar | `security.yml` |
| **Secret scanning + push protection** | Gizli açarın commit edilməsinə **mane olur** | GitHub, repo səviyyəsində |
| **Dependabot** | Asılılıq yeniləmələri və təhlükəsizlik xəbərdarlıqları | `dependabot.yml`, həftəlik |

Nəticələr **Security** tabında görünür. CodeQL qurulan kimi 5 real tapıntı verdi
(`actions/missing-workflow-permissions`) — hamısı düzəldilib.

## Server-authoritative dizayn

Layihə GPL-3.0-or-later-dür: dəyişdirilmiş client mənbədən qurula bilər. Ona görə
client-in saxladığı heç bir dəyər etibarlı sayılmır — health, damage, atəş
kadensiyası, recoil və hərəkət sürəti serverdə hesablanır (PRD 156).
Ətraflı: [ARCHITECTURE](docs/ARCHITECTURE.md).

## İnkişaf qaydaları

- Gameplay qərarları serverdə qalır; client paketi etibarsız girişdir.
- Paket ölçülərini, sayları, enum-ları, sonlu float dəyərlərini və sequence davranışını yoxlayın.
- JSON məzmununu və istifadəçi adlarını etibarlı əmrlər kimi icra etməyin.
- .env və credential-ları repo xaricində saxlayın.
- Əməliyyat mühitində registry yazılarını etibarlı server kimliyinə bağlamaq tələb olunur.
- Dependency audit nəticəsini build uğuru ilə qarışdırmayın.

[Mövcud məhdudiyyətlər](docs/KNOWN_ISSUES.md) və [operator bələdçisi](docs/wiki/Operations.md) bu release-in konkret sərhədlərini göstərir.
