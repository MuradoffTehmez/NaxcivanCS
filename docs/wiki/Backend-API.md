# Backend API

## Hazır xidmət

ASP.NET Core minimal API health, versiya və in-memory server registry təqdim edir. Account/auth, stats, matchmaking, PostgreSQL və Redis inteqrasiyası yoxdur. Swagger/OpenAPI endpoint-i də hazır quruluşa əlavə edilməyib.

## Lokal başlatma

```bash
dotnet run --project backend/NaxcivanCS.Backend.Api --no-launch-profile --urls http://127.0.0.1:8080
```

--no-launch-profile istifadə edilməsə launchSettings.json 38226 HTTPS və 38227 HTTP portlarını təyin edir. Nümunələr aşağıda 8080 üzərindən verilir.

## GET /health

```powershell
Invoke-WebRequest http://127.0.0.1:8080/health
```

Normal nəticə 200 və Healthy mətnidir. Bu check DB, Redis və ya game server health-i yoxlamır; onlar üçün xüsusi health check qeydiyyatı yoxdur.

## GET /api/v1/version

```json
{"protocolVersion":2,"gameVersion":"0.2.2","contentVersion":"0.2.2"}
```

Dəyərlər shared GameConstants-dən gəlir. Client/launcher inteqrasiyasının tam avtomatik olması bu endpoint-in mövcudluğundan çıxarılmamalıdır.

## POST /api/v1/servers/heartbeat

```powershell
$heartbeat = @{
  serverId = 'local-test-01'
  region = 'AzCaucasus'
  ip = '127.0.0.1'
  port = 27015
  capacity = 10
  players = 2
  status = 'ready'
  version = '0.2.2'
} | ConvertTo-Json
Invoke-WebRequest -Method Post -Uri http://127.0.0.1:8080/api/v1/servers/heartbeat -ContentType 'application/json' -Body $heartbeat
```

| Sahə | Tip | Məna |
|---|---|---|
| serverId | string | Registry açarı |
| region | string | Server region etiketi |
| ip | string | Elan edilən ünvan |
| port | int | Oyun UDP portu |
| capacity | int | Elan edilən tutum |
| players | int | Elan edilən oyunçu sayı |
| status | string | Status etiketi |
| version | string | Server build etiketi |

Uğurlu qəbul 202-dir. Boş serverId üçün 400 validation problem qaytarılır. Digər sahələrin semantik validasiyası, heartbeat authentication və rate limit yoxdur. Endpoint-i açıq internetdə etibarlı registry kimi istifadə etmək üçün əlavə iş tələb olunur.

## GET /api/v1/servers/

```powershell
Invoke-RestMethod http://127.0.0.1:8080/api/v1/servers/
```

Array qaytarılır. Hər element heartbeat sahələri və serverin yaratdığı UTC lastHeartbeat ehtiva edir. Son 30 saniyədə heartbeat olmayan entry nəticədən çıxır. Bu entry yaddaşdan avtomatik silinmir; proses restart-da bütün dictionary itir. Pagination və sabit sort müqaviləsi yoxdur.

## İnteqrasiya sərhədləri

Dedicated server bu endpoint-ə özü yazmır. Lokal nümunə yalnız API contract-ını sınaqdan keçirir. status/region sərbəst string olduğundan API serverin həqiqətən qoşulmağa hazır olduğunu yoxlamır.

[Mənbə: endpoints](../../backend/NaxcivanCS.Backend.Api/Endpoints/ServerRegistryEndpoints.cs) · [Contracts](../../backend/NaxcivanCS.Backend.Api/Contracts/Contracts.cs) · [Operations](Operations.md)
