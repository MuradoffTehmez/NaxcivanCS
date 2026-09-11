# Test və keyfiyyət

## Test qatları

| Qat | Nəyi sübut edir | Nəyi sübut etmir |
|---|---|---|
| Shared xUnit | Qayda, codec və hesablamaların verilmiş ssenaridə nəticəsi | Engine səhnəsi, render, tam gameplay |
| Client/server build | C# və Godot API uyğunluğu | Real oyun keyfiyyəti |
| Headless E2E | 2 client handshake, snapshot və təmiz çıxış | Kliklə atəş, audio, fairness |
| Backend smoke | Health, version, heartbeat/list contract-ı | Auth, DB və production yük |
| Manual playtest | Görünüş, input və hiss | Bütün sərhəd hallarının avtomatik təminatı |
| CI | Eyni commit-in təkrarlanan build/test nəticəsi | İcra olunmayan ssenarilər |

## Standart əmrlər

```bash
dotnet build NaxcivanCS.sln -c Release
dotnet test NaxcivanCS.sln -c Release --no-build
dotnet build server/NaxcivanCS.Server.csproj
dotnet build client/NaxcivanCS.Client.csproj
bash tools/e2e-smoke-test.sh
```

E2E-dən əvvəl Debug Godot assembly-ləri yenidən build olunmalıdır. Script Godot import edir, lakin .NET build-i özü etmir. GODOT_BIN və ya script-in ilk arqumenti executable yolunu verir.

## Mövcud test mövzuları

Damage/armor/falloff; economy loss bonus və alış; match qaydaları; silah JSON-u; movement; snapshot ölçüsü və truncation; codec round-trip; lag compensation/hitscan; weapon cadence, ammo, reload, recoil və semi-auto.

Protokol 2 keçidi üçün əvvəlki protocol 1-in qəbul edilməməsi də qorunur. Test sayını release hesabatından oxuyun; yeni test əlavə olunduqda statik “97” sayına bağlı acceptance qurmayın.

## E2E gedişi

Godot versiyası göstərilir, server/client asset import olunur, server 14 saniyə, client-lər 6 saniyəlik rejimdə başladılır. Server logunda dəqiq iki qəbul, hər client-də müsbət snapshot sayı, sıfır exit code və ERROR olmaması tələb olunur. Script bütün WARNING-ları fail etmir.

Loglar müvəqqəti qovluğa yazılır və çıxışda təmizlənir. Təhlil üçün konsol nəticəsini artifacts altında saxlayın.

## Manual qəbul ssenariləri

Pəncərə və HUD görünüşü, mouse capture/Esc, movement/jump/crouch/walk, ilk atış, spray, manual/auto reload, hitmarker, death/respawn, remote tracer və disconnect. Hər ssenaridə teq, mühit, gözlənilən/faktiki nəticə yazılmalıdır.

## Növbəti test sərhədi

Divar occlusion, friendly fire, ammo loss recovery, respawn reconciliation, 10 client, 50–200 ms gecikmə, paket itkisi və 60 dəqiqəlik soak əsas çatışmayan yoxlamalardır.

[Tam test planı](../TESTING.md) · [Release nəticəsi](../RELEASE-0.2.2.md)
