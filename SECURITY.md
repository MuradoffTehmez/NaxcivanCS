# Təhlükəsizlik

## Vəziyyət

0.2.2 araşdırma və lokal/LAN test prototipidir. Tam authentication, API rate limiting, heartbeat authorization və production moderasiya sistemi yoxdur. Server-authoritative dizayn bütün hücumlara qarşı zəmanət deyil.

## Dəstəklənən versiyalar

| Versiya | Dəstəklənir | Qeyd |
|---|---|---|
| 0.2.2 | :white_check_mark: | Cari əsas buraxılış |
| < 0.2.2 | :x: | Köhnə prototiplər üçün dəstək verilmir |

Cari inkişaf bazası main-dir. Əvvəlki teqlər üçün ayrıca təhlükəsizlik baxımı və cavab müddəti öhdəliyi elan edilməyib.

## Problem bildirmək

Məxfi zəiflik detalları, işlək exploit və tokenləri açıq issue/PR-də paylaşmayın. GitHub Security bölməsində private vulnerability reporting aktivdirsə həmin kanaldan istifadə edin. Aktiv deyilsə, əvvəlcə repo sahibindən məxfi kanal istəyən, texniki zəiflik təfərrüatı olmayan müraciət edin.

Hesabatda təsirlənən commit/teq, komponent, təsir, test mühiti, minimum reproduksiya və mümkün düzəliş olmalıdır. Şəxsi məlumatları və giriş sirlərini silin. İcazəsiz üçüncü tərəf serverlərini test etməyin.

## İnkişaf qaydaları

- Gameplay qərarları serverdə qalır; client paketi etibarsız girişdir.
- Paket ölçülərini, sayları, enum-ları, sonlu float dəyərlərini və sequence davranışını yoxlayın.
- JSON məzmununu və istifadəçi adlarını etibarlı əmrlər kimi icra etməyin.
- .env və credential-ları repo xaricində saxlayın.
- Əməliyyat mühitində registry yazılarını etibarlı server kimliyinə bağlamaq tələb olunur.
- Dependency audit nəticəsini build uğuru ilə qarışdırmayın.

[Mövcud məhdudiyyətlər](docs/KNOWN_ISSUES.md) və [operator bələdçisi](docs/wiki/Operations.md) bu release-in konkret sərhədlərini göstərir.
