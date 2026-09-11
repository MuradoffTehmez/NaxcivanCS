# Server əməliyyatları və Docker

> Gameplay/API nümunələri 0.2.2 tarixi snapshot-ına aiddir. Cari stable və unreleased dəyişikliklər üçün [README](../../README.md) və [CHANGELOG](../../CHANGELOG.md) əsasdır.

## Lokal topologiya

Client-lər birbaşa dedicated serverin UDP portuna qoşulur. Backend ayrıca HTTP prosesidir. PostgreSQL/Redis konteynerləri gələcək xidmətlər üçün başlanğıcdır; hazır API onların vəziyyətinə bağlı query etmir.

| Servis | Port | Qeyd |
|---|---|---|
| Game server | 27015/UDP | --port ilə dəyişir |
| Backend nümunəsi/Compose | 8080/TCP | Lokal HTTP |
| Launch profile | 38226 HTTPS / 38227 HTTP | dotnet run default profil |
| PostgreSQL Compose | 5432/TCP | Host-a publish edilir |
| Redis Compose | 6379/TCP | Host-a publish edilir |

## Lokal Docker stack

Repo kökündən:

```powershell
Copy-Item infrastructure/.env.example infrastructure/.env
```

.env içində öz lokal POSTGRES_PASSWORD dəyərinizi təyin edin. Faylı commit etməyin.

```bash
docker compose --env-file infrastructure/.env -f infrastructure/docker-compose.yml up -d postgres redis backend
docker compose --env-file infrastructure/.env -f infrastructure/docker-compose.yml ps
docker compose --env-file infrastructure/.env -f infrastructure/docker-compose.yml logs backend
```

Compose DB/cache healthcheck gözləyir; bu, backend-də həmin data xidmətlərinin inteqrasiya olunduğunu göstərmir. Public host üçün bazaların host portları və Redis auth vəziyyəti ayrıca yenidən hazırlanmalıdır.

## Game server image

```bash
docker build -f infrastructure/docker/gameserver.dockerfile -t naxcivancs-server:local .
docker run --rm -p 27015:27015/udp naxcivancs-server:local
```

Dockerfile Godot .NET və export templates yükləyir, Linux Server preset ilə export edir, config qovluğunu runtime-a kopyalayır. Bu yol repo tərəfindən təqdim edilir; 0.2.2 üçün ayrıca lokal Docker/export nəticəsi yalnız release hesabatında təsdiqlənibsə keçmiş sayılır. CI hazırda backend image build edir.

## Gündəlik diaqnostika

Əvvəl prosesin, sonra UDP portunun, sonra handshake/snapshot axınının vəziyyətini yoxlayın. “Port açıqdır” gameplay işləyir demək deyil. Backend /health də yalnız API health-check-dir.

Loglarda protocol/game version, map etiketi, silah sayı, player qəbul/disconnect və ERROR-lara baxın. Log paylaşmazdan əvvəl şəxsi ünvan və sirləri təmizləyin.

## Dayandırma və rollback

Birbaşa terminal serverini Ctrl+C ilə dayandırın. play.sh öz açdığı serveri client-lər bağlandıqdan sonra təmizləyir.

```bash
docker compose --env-file infrastructure/.env -f infrastructure/docker-compose.yml stop
```

Bu əmrlə volume-lar silinmir. Production rollback üçün əvvəlki yoxlanmış image və uyğun client build birlikdə seçilməlidir; protocol 1 və 2 qarışdırılmamalıdır. Git teqini dəyişdirib əvvəlki release-in tarixçəsini yenidən yazmayın.

## Gələcək operator işi

Authentication, heartbeat authorization, persistence, resource limit, crash telemetry, log retention, load test və server allocation tamamlanmalıdır. Bu sənəd production deployment sertifikasiyası deyil.

[Server](Dedicated-Server.md) · [API](Backend-API.md) · [Məhdudiyyətlər](../KNOWN_ISSUES.md)
