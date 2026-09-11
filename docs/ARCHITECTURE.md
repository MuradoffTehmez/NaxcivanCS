# NaxcivanCS — Arxitektura

Bu sənəd PRD 39–48, 86–101, 156-nın icra xəritəsidir.

## 1. Ümumi mənzərə

```text
        Windows Client (Godot 4 / C#)
              │
              │  input only  (UDP / ENet)
              ▼
       Dedicated Game Server (Godot headless / Linux / Docker)
              │
              ├── Movement validation
              ├── Weapon / fire-rate validation
              ├── Hit validation (lag compensated)
              ├── Economy
              ├── Round logic
              └── Anti-cheat scoring
              │
              │  HTTPS
              ▼
       Backend API (ASP.NET Core)
              │
              ├── PostgreSQL  (accounts, stats, matches, bans)
              └── Redis       (sessions, queues, party, server state)
```

## 2. Authority modeli (PRD 46, 156)

Client-in **göndərdiyi yeganə gameplay məlumatı**:

- `InputCommand` — hərəkət oxları, baxış bucağı, düymə maskası, sequence, timestamp
- `BuyRequest` — silah id-si
- chat / team select

Client **heç vaxt** göndərmir: `setHealth`, `giveMoney`, `giveWeapon`, `registerKill`,
`awardXP`, `setRank`, "mən X-i vurdum".

Bu qayda `shared/NaxcivanCS.Shared/Net/NetworkProtocol.cs` faylında `MessageType`
enum-u ilə struktur şəklində təsbit olunub: client→server mesajları 1–99,
server→client mesajları 100+ aralığındadır.

## 2b. Wire protokolu (PRD 41)

Godot-un yüksək səviyyəli RPC-si **istifadə edilmir**. Səbəb: client və server
ayrı layihələrdir (RPC eyni node yolunu tələb edir) və UDP paket ölçüsü üzərində
tam nəzarət lazımdır. Əvəzində xam ENet paketləri `PacketCodec` ilə oxunur.

Hər paket 2 baytlıq `MessageType` başlığı ilə başlayır:

| Növ | İstiqamət | Nəqliyyat | Məzmun |
|---|---|---|---|
| `Handshake` | client → server | reliable | protokol versiyası + username |
| `HandshakeAccepted` / `Rejected` | server → client | reliable | peer id, komanda / səbəb |
| `InputCommand` | client → server | **unreliable** | sequence, oxlar, yaw/pitch, düymə maskası |
| `WorldSnapshot` | server → client | **unreliable** | tick, server vaxtı, oyunçu siyahısı |
| `PlayerDamaged` / `PlayerKilled` | server → client | reliable | qurban, hücumçu, hitbox, damage |

Input və snapshot **unreliable** gedir: köhnəlmiş paketi yenidən göndərmək
gecikmə yaradar, növbəti paket onsuz da daha yeni vəziyyəti daşıyır.

Snapshot bir oyunçu üçün 32 bayt tutur; 10 oyunçuluq tam snapshot 334 baytdır —
tipik MTU-dan xeyli aşağı, fraqmentasiya olmur. Bu, unit testlə qorunur.

**Atəş ayrıca mesaj deyil.** `InputCommand`-dakı `PrimaryFire` biti və
yaw/pitch kifayətdir; server istiqaməti özü hesablayır. Beləliklə client
"mən vurdum" deyə bilmir (PRD 46).

## 3. Network dövrəsi (PRD 42–45)

| Mərhələ | Yer | Qeyd |
|---|---|---|
| Input toplama | Client | hər kadr |
| Client prediction | Client | dərhal vizual cavab |
| Input göndərmə | Client → Server | sequence nömrəli |
| Simulyasiya | Server | 64 tick (~15.6 ms) |
| Snapshot | Server → Client | dövri |
| Reconciliation | Client | server snapshot-ı ilə düzəliş |
| Interpolation | Client | digər oyunçular üçün ~100 ms gecikmə |
| Lag compensation | Server | 200 ms mövqe tarixçəsi |

## 4. Layihələr arası asılılıq

```text
NaxcivanCS.Shared  (net8.0, Godot-dan asılı DEYİL)
      ▲        ▲            ▲
      │        │            │
  Client   Server      Backend.Api
```

`Shared` Godot tipləri istifadə etmir — bu, həm backend-in, həm də unit testlərin
onu Godot olmadan yükləməsinə imkan verir.

### Shared-in tərkibi

| Fayl | PRD | Məzmun |
|---|---|---|
| `Enums/Enums.cs` | 7–8, 14, 19–22, 56, 60 | Team, RoundPhase, WeaponCategory, HitBox, RankTier, … |
| `Constants/GameConstants.cs` | 9–10, 13, 19, 25, 42, 45 | Round taymerləri, economy, tickrate, hitbox multiplier-ləri |
| `Constants/EconomyRules.cs` | 25–27 | Loss bonus pilləsi, satınalma validasiyası |
| `Constants/DamageRules.cs` | 18–20 | Damage, armor, falloff, spread |
| `Constants/MatchRules.cs` | 9–10 | MR12, overtime, side swap, faza müddətləri |
| `Constants/SuspicionRules.cs` | 47–48 | Violation çəkiləri, threshold-lar, server validator-ları |
| `Models/WeaponData.cs` | 16–17 | Silah statları + deterministik recoil pattern |
| `Models/PlayerState.cs` | 20, 53 | Server-authoritative oyunçu vəziyyəti |
| `Net/NetworkProtocol.cs` | 41, 46, 102 | Mesaj növləri, InputCommand, versiya qapısı |
| `Net/PacketCodec.cs` | 41, 46, 102 | Wire protokolu — encode/decode, ox klampı |
| `Net/Snapshot.cs`, `SnapshotSerializer.cs` | 44 | Kompakt binar snapshot formatı |
| `Gameplay/MovementSimulation.cs` | 11, 12, 43 | Deterministik hərəkət — client və serverdə eyni kod |
| `Gameplay/HitScan.cs` | 19, 46 | Hitbox həndəsəsi (Godot-suz, test olunan) |
| `Gameplay/LagCompensationBuffer.cs` | 45 | 200 ms mövqe tarixçəsi + rewind |
| `Config/WeaponCatalog.cs` | 155 | JSON-dan silah yükləmə |

## 4b. Prediction / reconciliation dövrəsi (PRD 43)

`MovementSimulation.Step()` client-də və serverdə **eyni koddur**. Axın:

```text
Client                                    Server
------                                    ------
input yığılır (sequence N)
  ├─ serverə göndərilir  ──────────────>  növbəyə qoyulur (tick başına maks 4)
  ├─ dərhal lokal tətbiq (prediction)     Step() + MoveAndSlide()
  └─ tarixçəyə yazılır                    mövqe lag-comp buferinə yazılır
                                          snapshot-da ackSeq = N qaytarılır
  <──────────────────────────────── snapshot
  ackSeq-ə qədər input-lar tarixçədən silinir
  fərq > 8 sm-dirsə:
    server mövqeyinə qayıt
    YALNIZ təsdiqlənməmiş input-ları yenidən oynat
```

`ackSeq` snapshot-un binar formatında hər oyunçu üçün daşınır — bu olmasaydı
client bütün tarixçəni yenidən oynadardı və düzəliş sıçrayışlı olardı.

Kolliziya hələ mühərrikdədir (`MoveAndSlide`), simulyasiya isə shared-dədir.
Tam deterministik kolliziya Phase 2-də shared-ə köçürüləcək.

## 5. Konfiqurasiya (PRD 155)

Balans rəqəmləri koda yazılmır. `config/` qovluğu build-dən kənar redaktə olunur:

- `config/weapons/*.json` — hər silah ayrıca fayl
- `config/server_default.json` — round, match, economy, anti-cheat parametrləri

`GameConstants` yalnız config yüklənmədikdə istifadə olunan default-ları saxlayır
və unit testlər hər iki mənbənin bir-birinə uyğunluğunu yoxlayır.

## 6. Versiyalaşdırma (PRD 102)

Üç ayrı versiya var:

- `ProtocolVersion` — network mesaj formatı; uyğunsuzluqda qoşulma rədd edilir
- `GameVersion` — build versiyası
- `ContentVersion` — asset paketləri

`VersionGate.IsCompatible()` handshake zamanı serverdə çağırılır.

## 7. Anti-cheat (PRD 47–48)

Müdafiənin əsası server-side-dır. `ServerValidators` sinfi sərt yoxlamaları verir
(`IsImpossibleMovement`, `IsImpossibleFireRate`, `IsPickupInRange`), `SuspicionRules`
isə bal toplayır:

```text
80  → review
100 → müvəqqəti məhdudiyyət
```

Avtomatik permanent ban ilk mərhələdə YOXDUR.

## 8. Backend (PRD 88–98)

MVP-də yalnız iki endpoint qrupu qurulub:

- `GET  /api/v1/version` — versiya qapısı
- `POST /api/v1/servers/heartbeat`, `GET /api/v1/servers` — game server registry (PRD 98)
- `GET  /health` — KPI monitorinqi (PRD 116)

Registry hazırda in-memory-dir; Phase 5-də Redis-ə köçürüləcək (PRD 91).
PostgreSQL sxemi PRD 92–97-də verilib və Phase 4-də tətbiq olunacaq.

## 9. Server lifecycle (PRD 101)

```text
Backend server tələb edir → Container start → Server registry-ə qeydiyyat
→ Oyunçular qoşulur → Match → Stats upload → Replay upload → Container destroy
```
