# Test planı

## Avtomatik baza

```bash
dotnet build NaxcivanCS.sln -c Release
dotnet test NaxcivanCS.sln -c Release --no-build --collect:"XPlat Code Coverage" --settings coverage.runsettings --results-directory TestResults
dotnet format NaxcivanCS.sln --verify-no-changes --no-restore
pwsh -NoProfile -File tools/check-coverage.ps1
dotnet build server/NaxcivanCS.Server.csproj
dotnet build client/NaxcivanCS.Client.csproj
bash tools/e2e-smoke-test.sh
```

GODOT_BIN təyin edilməlidir. Son mənbəni build etmədən --no-build nəticəsinə güvənməyin. E2E Godot import edir, C# build etmir.

## Test əhatəsi və coverage

Mövcud Shared.Tests saxlanır. Round/scoreboard wire round-trip, UTF-8, truncation və sərhəd testləri əlavə edilib. Backend integration testləri ASP.NET Core `WebApplicationFactory` ilə Production mühitində health/version, registry register/update, boş ID, malformed JSON, media type və 404 cavablarını yoxlayır. Real şəbəkə portu və DB tələb etmir; unikal server ID-ləri statik in-memory registry testlərini ayırır. 30 saniyəlik expiry və auth/DB inteqrasiyası bu testlərin əhatəsində deyil.

Server.Tests Godot-dan asılı olmayan `ServerPlayer.cs` və `HitValidator.cs` fayllarını link vasitəsilə birbaşa compile edir. Runtime faylları köçürülmür. Input flood limiti, köhnə sequence, damage/respawn/snapshot, ən yaxın canlı hədəf, lag rewind və miss davranışı yoxlanır.

`coverlet.collector` Cobertura XML yaradır. `tools/check-coverage.ps1` source path + line üzrə hesabatları birləşdirir və Shared, Backend, test olunan server siniflərinin **hər biri üçün minimum 70% line coverage** tələb edir. Test kodu və generasiya olunan kod sayılmır. Bu faiz bütün Godot render/ENet/runtime coverage-si deyil. Hesabat yoxdursa gate uğursuz olur. Lokal ölçmələr üçün təmiz nəticə qovluğu seçin; əvvəlki run-ların coverage-si qarışdırılmamalıdır.

CI XML/TRX və `coverage-summary.md` fayllarını artifact kimi saxlayır. Coverage/DCO/versiya/lisenziya skriptlərinin müsbət və mənfi halları `tools/test-quality-gates.ps1` ilə yoxlanır.

## Sənəd yoxlaması

```powershell
pwsh -NoProfile -File tools/check-doc-links.ps1
pwsh -NoProfile -File tools/publish-wiki.ps1 -SourceRef (git rev-parse HEAD)
```

Link aləti relative fayl/link hədəflərini və kod çəpərlərinin bağlanmasını yoxlayır. Xarici HTTP resurslarının mövcudluğunu və Markdown heading anchor-larını yoxlamır. Wiki export nəşrsiz yoxlana bilər.

## Manual gameplay matrisi

| Ssenari | Gözlənilən müşahidə |
|---|---|
| 2 pəncərə | İki fərqli peer, Alpha/Bravo, remote model |
| Capture/Esc | Klik mouse-u tutur, Esc buraxır |
| Movement | WASD, jump, crouch və walk cavab verir |
| İlk güllə | Ammo 1 azalır, tracer/muzzle flash |
| Spray | Server kadensiyası və recoil görünür |
| R reload | Qismən şarjor doldurulur, reload zamanı atəş yoxdur |
| Avtomatik reload | Boş şarjor və reserve varsa başlayır |
| Damage | Server health dəyişir, shooter hitmarker görür |
| Ölüm/respawn | main-də növbəti round başında health 100, armor 0; stable tarixçədə auto-respawn |
| Disconnect | Qarşı tərəfdə remote model silinir |

Ammo UI, occlusion və digər [known issue](KNOWN_ISSUES.md)-lar hesabatda ayrıca qeyd edilməlidir. Testin “keçdi” statusu müşahidə edilən meyara bağlıdır; tamamlanmamış funksiyaya yaşıl status verilməməlidir.

## Şəbəkə/yük matrisi — gələcək

10 client, 50/100/200 ms RTT, packet loss, paket sırasının dəyişməsi, reconnect və 60 dəqiqəlik soak. Tick müddətinin median/p95/p99, snapshot intervalı, reconciliation sayı, RSS memory və crash nəticəsi saxlanmalıdır.

## Backend smoke

Lokal --no-launch-profile --urls ilə başlayın. /health=200, /version gözlənilən versiya, valid heartbeat=202, blank serverId=400, list-də entry və 30 saniyəlik timeout filtri yoxlanmalıdır. Bu testlər DB/auth inteqrasiyasını əhatə etmir.

## CI

backend-and-shared, godot-projects, e2e-smoke-test, dependency audit və main/develop üçün backend Docker build mövcuddur. Format məcburidir: dəyişiklik tələb olunarsa CI uğursuz olur. Release üçün konkret commit-in CI nəticəsinə baxın.

[0.2.2 nəticələri](RELEASE-0.2.2.md) · [Wiki Testing](wiki/Testing.md)
