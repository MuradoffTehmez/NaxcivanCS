# Yol xəritəsi

Cari stable buraxılış hələ prototip mərhələsindədir. Versiya adı mərhələlərin hamısının bitdiyini göstərmir. PRD-dəki “Version 1.0” bölməsi tarixi məhsul milestone adıdır.

## Hazır baza

- Shared modellər, damage/economy/match qaydaları və silah kataloqu.
- Dedicated ENet server və Godot client.
- Prediction, reconciliation, interpolation və lag compensation başlanğıcı.
- Server kadensiyası, ammo/reload və deterministik recoil.
- Silah view model-i, tracer, muzzle flash, hitmarker və HUD.
- Prosedural audio: atəş, reload, impact və səth əsaslı spatial addım səsləri.
- Round sistemi: Freeze/Buy/Active/RoundEnd, MR12 skor, side swap, round economy, scoreboard.
- Bomb/Defuse: A/B site, plant/defuse, bomba düşmə-götürmə, partlayış və defuse qalibiyyəti.
- Backend versiya/registry skeleti; CI və iki client smoke testi.

## Növbəti mərhələ — core etibarlılığı

- [ ] Divar occlusion, friendly fire və hitscan məsafə qaydaları.
- [ ] Tick üzrə movement büdcəsi və möhkəm input validasiyası.
- [ ] Ammo/respawn sinxronlaşdırılması və reconciliation təkmilləşməsi.
- [ ] Faktiki spread/movement balans inteqrasiyası (round taymerləri artıq config-dən oxunur).
- [ ] 10 client, latency/loss və 60 dəqiqəlik sabitlik ölçümü.
- [ ] Əl ilə gunplay hissinin, render və səsin yoxlanması.

## Multiplayer və Bomb Mode

- [x] Round reset, scoreboard və komanda lifecycle-ı (5v5 yük altında sınanmayıb).
- [x] MR12 skor və side swap. **Overtime** yalnız vəziyyət kimi qeyd olunur — PRD-dəki 3 hücum / 3 müdafiə formatı hələ yoxdur.
- [x] Plant/defuse, A/B site, victory şərtləri (blockout site-ları; real xəritə zonaları Phase 2-dədir).
- [ ] Buy menu, inventory, weapon switch/drop/pickup.
- [ ] Economy inteqrasiyası, grenade və server smoke/flash.

## Online xidmətlər

- [ ] PostgreSQL sxemi və migration.
- [ ] Account/authentication, profile, stats və match history.
- [ ] Redis session/party/queue və etibarlı server registry.
- [ ] Region/ping/MMR matchmaking və server allocation.
- [ ] Ranking, moderation, report və telemetry.

## Məzmun və buraxılış keyfiyyəti

- [ ] NC_Qala, NC_Duzdag və NC_Araz real xəritələri.
- [ ] Audio, localization, settings və accessibility.
- [ ] Closed alpha/beta acceptance ölçümləri.
- [ ] Launcher/patch, replay, cosmetics və leaderboard.

Hər mərhələ üçün qəbul meyarı işləyən axın və ölçülən test olmalıdır. Xəritə ideyaları [Maps](wiki/Maps.md), məhsul məqsədi [Game-Overview](wiki/Game-Overview.md), mövcud risklər [PROJECT_ANALYSIS](PROJECT_ANALYSIS.md) sənədindədir.
