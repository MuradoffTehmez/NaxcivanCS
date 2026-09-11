# NaxcivanCS

> Competitive Multiplayer Tactical FPS — Godot 4 + C#, server-authoritative dedicated server.

**Status:** Pre-production → Phase 0/1 (Prototype 0.1)
**Versiya:** 0.1.0
**Sənəd:** [docs/PRD.md](docs/PRD.md)

---

## Nədir?

NaxcivanCS — 5v5 Bomb/Defuse rejimli, aşağı giriş baryerinə malik rəqabətli FPS.
Naxçıvan memarlığı və coğrafiyasından ilhamlanan **original** xəritələr, öz vizual
kimliyi və gameplay balansı ilə müstəqil məhsuldur (PRD 144 — heç bir Counter-Strike
və ya Valve asset-i, kodu, xəritəsi istifadə edilmir).

Əsas texniki prinsip (PRD 156): **client heç vaxt gameplay nəticəsinin qərarvericisi deyil.**

## Repozitoriya strukturu

| Qovluq | Təyinat | PRD |
|---|---|---|
| `client/` | Godot 4 C# oyun client-i | 86 |
| `server/` | Godot headless dedicated server | 87 |
| `backend/` | ASP.NET Core API (accounts, stats, matchmaking) | 88 |
| `shared/` | Client/server/backend arasında paylaşılan modellər, enum-lar, qaydalar | 153 |
| `config/` | Silah balansı və server config (JSON — build dəyişmədən redaktə olunur) | 155 |
| `localization/` | `az.json`, `en.json`, `ru.json` | 118 |
| `infrastructure/` | Docker, docker-compose | 99–101 |
| `tests/` | Unit testlər (economy, damage, match state, anti-cheat) | 124 |
| `docs/` | PRD, arxitektura, yol xəritəsi | — |
| `launcher/` | Gələcək launcher (Phase 8) | 103 |
| `tools/` | Köməkçi skriptlər | — |

## Tələblər

| Alət | Versiya | Qeyd |
|---|---|---|
| .NET SDK | 8.0+ | Bütün C# layihələri `net8.0` hədəfləyir |
| Godot | 4.7.2 **Mono/.NET** | Client və server üçün |
| Docker | 24+ | Yalnız backend/gameserver konteyneri üçün — opsional |
| PostgreSQL | 16 | Phase 4-dən etibarən |
| Redis | 7 | Phase 5-dən etibarən |

Godot quraşdırma (Windows):

```bash
winget install --id GodotEngine.GodotEngine.Mono --exact
```

## Başlanğıc

Build və test:

```bash
dotnet build NaxcivanCS.sln
```

```bash
dotnet test NaxcivanCS.sln
```

Dedicated serveri lokal işə salmaq (PRD 87):

```bash
godot --headless --path server -- --port 27015 --map NC_Qala
```

Client-i editor-da açmaq:

```bash
godot --path client
```

Backend API:

```bash
dotnet run --project backend/NaxcivanCS.Backend.Api
```

Backend + PostgreSQL + Redis (Docker):

```bash
cd infrastructure && cp .env.example .env && docker compose up -d postgres redis backend
```

## Development prioriteti (PRD 151)

```
Networking → Movement → Gunplay → Hit Registration → Round Logic
→ Bomb → Economy → Maps → Backend → Matchmaking → Ranking → Cosmetics
```

Store, skin, animasiya və vebsayt üzərində işləyib core gunplay-i gecikdirmək
layihənin ən böyük riskidir.

## Yol xəritəsi

Bax: [docs/ROADMAP.md](docs/ROADMAP.md) və [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Git workflow (PRD 122)

```
main ← release/* ← develop ← feature/* | fix/*
```

Hər dəyişiklik `develop`-dan branch edilir, PR açılır, CI keçir, sonra merge olunur.

## Lisenziya və IP

NaxcivanCS müstəqil IP-dir. Counter-Strike xəritələrinin reproduksiyası, Valve
asset-ləri, CS audio/logo/model-ləri və decompiled oyun kodu **qadağandır** (PRD 144).
