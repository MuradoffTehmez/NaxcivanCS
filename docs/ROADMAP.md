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
- [ ] Movement prototipi

## Phase 1 — Prototype 0.1  (PRD 127, 152)

Milestone tələbləri:

- [ ] 1 test map (block-out)
- [ ] 2 oyunçu, ENet üzərindən
- [ ] Movement + mouse look
- [ ] 1 tüfəng, atəş
- [ ] Health / damage / death / respawn
- [ ] Ping göstəricisi

**Sual:** *"Gunplay fun-dırmı?"* — cavab "yox"dursa, növbəti fazaya keçilmir.

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
| 1 | 10 oyunçu serverə qoşula bilir | ⬜ |
| 2 | 5v5 stabil oynanılır | ⬜ |
| 3 | Server-authoritative movement işləyir | ⬜ |
| 4 | Weapon firing düzgün sync olunur | ⬜ |
| 5 | Hit registration sabitdir | ⬜ |
| 6 | Bomb plant/defuse işləyir | ⬜ |
| 7 | Round reset problemsizdir | ⬜ |
| 8 | Economy düzgün hesablanır | 🟡 qaydalar + testlər hazırdır |
| 9 | Server 60+ tick stabil işləyir | ⬜ |
| 10 | 60 dəqiqəlik testdə critical crash yoxdur | ⬜ |

## Xəritələr (PRD 31)

| Xəritə | Tema | Gameplay | Status |
|---|---|---|---|
| NC_Qala | Naxçıvan memarlığı, daş küçələr | Medium range, balanced | ⬜ MVP hədəfi |
| NC_Duzdag | Duzdağ, tunellər, underground | Close-medium, vertical | ⬜ |
| NC_Araz | Warehouse, dəmir yolu, cargo | Long sightlines, sniper | ⬜ |
| NC_Ordubad | Həyətlər, bağlar | Tactical | ⬜ |
| NC_Batabat | Dağ, təbiət | Casual / Deathmatch | ⬜ |
