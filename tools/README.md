# Tools

Köməkçi skriptlər və development alətləri. Əmrləri repo kökündən işlədin.

| Alət | İstifadə |
|---|---|
| `play.sh [1\|2]` | Build, lokal server və client pəncərələri |
| `run-server.sh [port]` | Server build və headless başlanğıc |
| `run-client.sh [name] [port]` | Client build və localhost bağlantısı |
| `godot-env.sh` | GODOT_BIN/PATH/winget üzrə Godot aşkarlanması |
| `e2e-smoke-test.sh [godot]` | Əvvəlcədən build edilmiş server + 2 client testi |
| `check-doc-links.ps1` | Relative Markdown fayl linkləri və code fence yoxlaması |
| `publish-wiki.ps1` | Wiki export; yalnız `-Publish` ilə GitHub-a push |

PowerShell alətləri versiya 7 tələb edir. Link yoxlaması `rg`, o yoxdursa `git ls-files` istifadə edir. Wiki publish əsas reponun Git müəllif məlumatını götürür, fresh clone yaradır və force-push/silinmə etmir.

```powershell
pwsh -NoProfile -File tools/check-doc-links.ps1
pwsh -NoProfile -File tools/publish-wiki.ps1 -SourceRef (git rev-parse HEAD)
pwsh -NoProfile -File tools/publish-wiki.ps1 -SourceRef (git rev-parse HEAD) -Publish
```

Export nəticəsi `artifacts/wiki` altındadır. GitHub-da ilk Wiki səhifəsi əvvəlcə yaradılmalıdır. Tam təlimat: [Release](../docs/RELEASING.md), [Testing](../docs/TESTING.md).

Planlaşdırılan:

- `balance/` — silah balans müqayisə cədvəli generatoru (config/weapons JSON-larından)
- `maps/` — callout xəritəsi generatoru (PRD 33)
- `telemetry/` — network test ssenariləri (PRD 125: 20–200 ms latency, 0–5% loss)

## Quality/release alətləri

- `sync-version.ps1`: Directory.Build.props/global.json əsasında istinadları yeniləyir; `-Check` drift-i rədd edir, `-ReleaseTag` stable teqi təsdiqləyir.
- `check-coverage.ps1`: Cobertura nəticələrini birləşdirir, hər test olunan qat üçün 70% gate tətbiq edir.
- `check-dco.ps1`: base/head SHA arasındakı bütün commit-lərin müəllif sign-off-unu yoxlayır.
- `test-quality-gates.ps1`: coverage/DCO mənfi və müsbət fixture-ləri.
- `check-vulnerabilities.ps1`: solution və Godot layihələrinin JSON dependency audit-i; aşkarlama və alət xətaları fail edir.

SDK `global.json`-da dəqiq pinlidir; `dotnet --version` uyğun olmalıdır. Test və buraxılış komandaları [TESTING](../docs/TESTING.md) və [RELEASING](../docs/RELEASING.md) sənədlərindədir.
