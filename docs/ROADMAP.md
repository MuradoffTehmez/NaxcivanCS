# NaxcivanCS — Yol xəritəsi

PRD 126–138 əsasında. Hər faza öz acceptance kriteriyası ilə bağlıdır.

## Phase 0 — Pre-production  ✅ (bu repozitoriya)

- [x] PRD tamamlandı (`docs/PRD.md`)
- [x] Arxitektura sənədi (`docs/ARCHITECTURE.md`)
- [x] Repozitoriya strukturu (PRD 121, 153)
- [x] Shared domain layer: economy, damage, match, anti-cheat qaydaları
- [x] Silah konfiqurasiya sistemi (PRD 155) + 8 MVP silahı
- [x] Network protokol skeleti + versiya qapısı (PRD 41, 102)
- [x] Godot client/server layihələri (ENet bağlantısı)
- [x] Backend API skeleti + server registry (PRD 98)
- [x] Unit testlər (PRD 124) — 38 test
- [x] CI/CD pipeline (PRD 123)
- [x] Docker infrastrukturu (PRD 99–101)
- [x] Lokalizasiya faylları (PRD 118)
- [ ] Art direction sənədi

## Phase 1 — Prototype 0.1  🟡 (PRD 127, 152)

Milestone tələbləri:

- [x] 1 test map (block-out: döşəmə, 3 örtük, 4 divar)
- [x] 2 oyunçu, ENet üzərindən — **e2e testdə təsdiqləndi**
- [x] Movement + mouse look (client prediction + server reconciliation)
- [x] 1 tüfəng, atəş (server-authoritative hitscan, lag compensation)
- [x] Health / damage / death / respawn
- [x] Ping göstəricisi
- [x] Silah view model, geri-təpmə və sway
- [x] Recoil (PRD 17 — deterministik pattern, ilk güllə dəqiq)
- [x] Muzzle flash, tracer, impact izi
- [x] Hit marker və dinamik crosshair
- [x] Patron, reload, avtomatik/yarımavtomatik atəş
- [ ] Atəş və vurulma **səsləri** (PRD 83 — hələ yoxdur)
- [ ] Əllə oynanış testi (`--headless` deyil, real input ilə)

### Nə işləyir

`tools/e2e-smoke-test.sh` hər CI run-ında yoxlayır:

```text
Dedicated server qalxır (64 tick, 8 silah config-dən yüklənir)
  ↓
2 client qoşulur, handshake protokol versiyası ilə yoxlanılır
  ↓
Komandalar avtomatik bölünür (Alpha / Bravo)
  ↓
~32 Hz snapshot axını — 6 saniyədə hər client ~188 snapshot alır
  ↓
Təmiz disconnect, 0 ERROR, 0 WARNING
```

**Sual:** *"Gunplay fun-dırmı?"* — bu, yalnız əllə oynanış testindən sonra
cavablandırıla bilər. Texniki boru kəməri hazırdır; his hələ yoxlanmayıb.

## Phase 2 — Multiplayer Core  (PRD 128)

- [ ] Dedicated server tam ayrılması
- [ ] 5v5
- [ ] Round sistemi (`RoundController` genişləndirilir)
- [ ] Komandalar, weapon sync, damage, scoreboard
- [ ] Client prediction + reconciliation (PRD 43)
- [ ] Snapshot interpolation (PRD 44)
- [ ] Lag compensation (PRD 45)

## Phase 3 — Bomb Mode  (PRD 129)

- [ ] Bomba, plant, defuse, A/B site-lar
- [ ] Round victory şərtləri
- [ ] Economy + buy menu (shared qaydalar artıq hazırdır)
- [ ] Smoke/flash serverdə yaradılır (PRD 23, 24)

## Phase 4 — Backend  (PRD 130)

- [ ] PostgreSQL sxemi (PRD 92–97)
- [ ] Argon2id ilə authentication (PRD 111)
- [ ] Profil, stats, match history
- [ ] Friends, party (Redis)

## Phase 5 — Matchmaking  (PRD 131)

- [ ] Queue, MMR, region, ping
- [ ] Server allocation + registry-nin Redis-ə köçürülməsi
- [ ] Competitive ranking

## Phase 6 — Alpha  (PRD 132, 138)

- [ ] 3 xəritə, 10–15 silah
- [ ] Competitive / Casual / Deathmatch
- [ ] Admin panel, basic anti-cheat
- [ ] `>99%` match completion, `<2%` server crash, `<3%` client crash

## Phase 7 — Closed Beta  (PRD 133)

- [ ] 100–1000 oyunçu
- [ ] Server load, matchmaking, balance, crash telemetry testləri

## Phase 8 — Open Beta  (PRD 134)

- [ ] Launcher, store, cosmetics, season, leaderboard, replay

## Version 1.0  (PRD 135)

- [ ] 5+ competitive xəritə, 20+ silah
- [ ] Matchmaking, ranking, anti-cheat, party, friends, stats, replay, moderation
- [ ] Scalable dedicated serverlər

---

## MVP acceptance kriteriyaları (PRD 137)

| # | Kriteriya | Status |
|---:|---|---|
| 1 | 10 oyunçu serverə qoşula bilir | 🟡 2 oyunçu e2e-də təsdiqləndi, 10 sınanmayıb |
| 2 | 5v5 stabil oynanılır | ⬜ |
| 3 | Server-authoritative movement işləyir | ✅ prediction + reconciliation işləyir |
| 4 | Weapon firing düzgün sync olunur | 🟡 server hesablayır + vizual hazır, audio yoxdur |
| 5 | Hit registration sabitdir | 🟡 lag comp + hitscan hazır, ping altında sınanmayıb |
| 6 | Bomb plant/defuse işləyir | ⬜ Phase 3 |
| 7 | Round reset problemsizdir | ⬜ Phase 2 |
| 8 | Economy düzgün hesablanır | 🟡 qaydalar + testlər hazırdır, bağlanmayıb |
| 9 | Server 60+ tick stabil işləyir | 🟡 64 tick qalxır, yük altında ölçülməyib |
| 10 | 60 dəqiqəlik testdə critical crash yoxdur | ⬜ uzun test aparılmayıb |

## Xəritələr (PRD 31)

| Xəritə | Tema | Gameplay | Status |
|---|---|---|---|
| NC_Qala | Naxçıvan memarlığı, daş küçələr | Medium range, balanced | ⬜ MVP hədəfi |
| NC_Duzdag | Duzdağ, tunellər, underground | Close-medium, vertical | ⬜ |
| NC_Araz | Warehouse, dəmir yolu, cargo | Long sightlines, sniper | ⬜ |
| NC_Ordubad | Həyətlər, bağlar | Tactical | ⬜ |
| NC_Batabat | Dağ, təbiət | Casual / Deathmatch | ⬜ |
