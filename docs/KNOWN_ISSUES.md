# Məlum problemlər və məhdudiyyətlər — 0.2.2

> Bu sənəd 0.2.2 tarixi snapshot-ıdır. Cari stable və unreleased vəziyyət üçün [README](../README.md) və [CHANGELOG](../CHANGELOG.md) əsasdır.

Bu siyahı kod baxışına əsaslanır. Buradakı hər müşahidə avtomatik testlə reproduksiya olunmuş bug demək deyil; yoxlama növü ayrıca göstərilir.

| ID | Müşahidə və təsir | Sübut / yoxlama növü | Növbəti addım |
|---|---|---|---|
| KI-01 | Hitscan örtük/divar occlusion yoxlamır; divar arxasındakı oyunçu vurula bilər | HitValidator.Evaluate yalnız oyunçu hitbox-larını yoxlayır; statik kod baxışı | İlk dünya kolliziyasına qədər hit məsafəsini məhdudlaşdırmaq |
| KI-02 | Friendly fire false JSON-u tətbiq edilmir; hədəflərdə team filtri yoxdur | HitValidator və server_default.json; kod baxışı | Config loader və server team qaydasını inteqrasiya etmək |
| KI-03 | Ammo HUD ilk spawn/respawn və itən paketdən sonra köhnələ bilər | WeaponState yalnız shot/reload hadisəsində unreliable göndərilir | İlkin/respawn sync, periodik və ya reliable düzəliş |
| KI-04 | server_default.json runtime-da oxunmur | ServerMain/GameWorld config yolu yalnız silahlar üçündür | Tipli config loader, validasiya və tətbiq testləri |
| KI-05 | Hərəkətdə input limiti ümumi vaxt büdcəsi deyil | GameWorld hər növbə input-una tam delta verir | Tick üzrə vahid movement büdcəsi və regressiya testi |
| KI-06 | Spread və MovementSpeed sahələri faktiki atəş/hərəkət yoluna tam qoşulmayıb | GameWorld aim yalnız recoil əlavə edir, MovementSimulation sabit sürətlər istifadə edir | Data-driven balans inteqrasiyası |
| KI-07 | Yeni tam spawn durumu velocity/grounded daxil olmaqla reconciliation ilə bütöv ötürülmür | Snapshot yalnız məhdud sahələr daşıyır | Tam state reconciliation və respawn integration testi |
| KI-08 | Backend registry restart-da silinir, auth/TTL təmizləyici yoxdur | ConcurrentDictionary; list köhnə entry-ni yalnız filtr edir | Persistence və etibarlı heartbeat qəbul axını |
| KI-09 | RoundController gameplay lifecycle-a qoşulmayıb | ServerMain scene quruluşu | Round/bomb/economy inteqrasiyası |
| KI-10 | Səs və localization runtime inteqrasiyası yoxdur | Audio qovluğu placeholder, HUD-da literal string-lər | Audio pipeline və localization service |
| KI-11 | Landing slowdown gözlənilən keçidi tutmur | MovementSimulation.Step eyni IsGrounded dəyərindən həm əvvəlki, həm cari vəziyyət kimi istifadə edir | Əvvəlki grounded state və eniş testi |
| KI-12 | 10 oyunçulu, packet-loss və uzunmüddətli yük təsdiqi yoxdur | Mövcud E2E iki client handshake/snapshot yoxlayır | 10 client, latency/loss və 60 dəqiqəlik soak testi |

## Bunlar da hələ təqdim edilmir

Weapon switching, buy menu, bomb plant/defuse, tam scoreboard, text/voice chat, bot, training mode, matchmaking, account/profile, ranking, replay, store və launcher. Enum, JSON və boş qovluq funksiyanın işlədiyinin sübutu deyil.

## Operator üçün

0.2.2-ni nəzarətli test şəbəkəsində sınaqdan keçirin. Compose bazaları host portlarında açır; bu tərif public production yerləşdirmə planı deyil. Backend /health yalnız prosesin health-check nəticəsini verir, DB/Redis və oyun keyfiyyətini yoxlamır.

## Test sərhədi

Unit testlər shared hesablamaları yoxlayır. Headless E2E real qrafika, kliklə atəş, səs, fairness, reconnect və uzunmüddətli sabitliyi əvəz etmir. Buraxılış yoxlaması [RELEASE-0.2.2](RELEASE-0.2.2.md) sənədindədir.
