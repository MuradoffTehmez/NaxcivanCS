# Buraxılış proseduru

Branch modeli dəyişmir: `feature/*` / `fix/*` / `codex/*` → `develop` → `release/*` → `main` → prefikssiz teq. Solo development məcburi reviewer tələb etmir; quality gate-lər keçməlidir.

## Versiya mənbəyi

`Directory.Build.props` yeganə məhsul versiyası mənbəyidir:

- `NaxcivanCSStableVersion`: ən son yayımlanmış stable; CITATION və SECURITY bunu göstərir.
- `NaxcivanCSVersion`: checkout-un build/game/content versiyası; `-dev` buraxılmamış inkişafı göstərir.
- `NaxcivanCSReleaseDate`: stable buraxılış tarixi.

Dəyərləri dəyişdikdən sonra `pwsh -NoProfile -File tools/sync-version.ps1` işlədin. Skript GameConstants, Godot layihələri, Windows numeric metadata, README, CITATION və SECURITY-ni sinxronlaşdırır. Bu törəmə istinadları ayrıca redaktə etməyin. CI `-Check` ilə drift-i rədd edir. Keçmiş CHANGELOG qeydləri və `RELEASE-0.2.2.md` kimi tarixi hesabatlar dəyişdirilmir.

`ProtocolVersion` ayrıca wire compatibility müqaviləsidir; məhsul versiyası ilə avtomatik artırılmır. Wire dəyişiklikləri üçün köhnə client/server uyğunluğunu ayrıca yoxlayın. `global.json` SDK versiyasının mənbəyidir; Docker SDK default-ları da sinxronizasiya olunur. Lokal və CI SDK roll-forward etmir.

## Stable release hazırlığı

1. Mövcud qayda ilə `develop`-dan `release/X.Y.Z` yaradın.
2. Props-da build və stable versiyalarını eyni `X.Y.Z` edin, buraxılış tarixini yazın və sync skriptini işlədin. `-dev` build yayımlana bilməz.
3. CHANGELOG-un uyğun unreleased qeydlərini `## X.Y.Z — YYYY-MM-DD` başlığı altına köçürün. README-də stable/unreleased funksiyaların izahını yeniləyin.
4. [Test planındakı](TESTING.md) build, test, format, coverage, E2E, metadata və sənəd yoxlamalarını işlədin. Bütün PR commit-ləri, o cümlədən merge commit-ləri `git commit -s` ilə sign-off tələb edir.
5. Mövcud PR/branch qaydası ilə `main`-ə inteqrasiya edin; `develop` sinxronunu [Wiki workflow](wiki/Release-Workflow.md) qaydasına uyğun saxlayın.
6. Prefikssiz teqi yalnız həmin commit-ə yaradın və göndərin. Köhnə stable teqi yenidən yazmayın.

```powershell
$version = 'X.Y.Z' # Hazırladığınız real versiya ilə əvəz edin.
pwsh -NoProfile -File tools/sync-version.ps1 -Check -ReleaseTag $version
git tag -a $version -m "NaxcivanCS $version"
git push origin "refs/tags/$version"
```

## Avtomatik artifact pipeline

`release.yml` teq push-u və mövcud release-in `published` hadisəsi ilə işləyir. Versiya/CHANGELOG uyğunluğu və teqin `main` tarixçəsinə aid olması yoxlanır. Eyni commit üçün mövcud CI, Security və CodeQL reusable workflow-ları yenidən işləyir; uğursuz və ya ləğv edilmiş yoxlama export/publish-i bloklayır. Dependency review PR zamanı qalır; release-də bütün solution/client/server asılılıqları ayrıca audit olunur. CodeQL workflow uğuru analiz/upload uğurudur; tapıntıların triage-ı GitHub Security-də aparılır.

Godot export-u üçün client/server yanında ayrıca `.sln` faylları saxlanır; root solution-a Godot layihələri əlavə olunmur. Server preset-ində S3TC aktivdir (export validator bunu tələb edir), .NET output-ları isə SBOM-da görünməsi üçün ayrıca `data_*` qovluğunda saxlanır. Paketləri yalnız executable-ı götürərək dağıtmayın.

Mövcud Godot preset-lərindən iki paket hazırlanır:

- `NaxcivanCS-X.Y.Z-client-windows-x64.tar.gz`
- `NaxcivanCS-X.Y.Z-server-linux-x64.tar.gz`

Hər paketdə `game/`, onun yanında `config/`, LICENSE, README və source commit keçidi saxlanır. Bu qovluq quruluşunu qoruyun. Server export-u headless smoke test keçir. macOS/ARM/Web hazırkı release matrisi deyil.

Hər paket üçün SPDX JSON SBOM, dəqiq commit-in source arxivi və bütün payload-lar üçün `SHA256SUMS.txt` əlavə olunur. SBOM üçün yalnız release aləti olan Syft/Anchore action istifadə edilir; gameplay/backend runtime dependency-si deyil. Godot editor/templates upstream SHA512 siyahısı ilə doğrulanır. Yalnız publish job-u `contents: write` alır. Tag axınında əvvəl draft hazırlanır, fayllar yüklənəndən sonra release yayımlanır. Əl ilə əvvəlcədən published release yaradılıbsa, asset-lər yoxlamalardan sonra əlavə edilir.

```bash
sha256sum --check SHA256SUMS.txt
```

Workflow-nu uğursuz run-dan təkrar işə salmaq mümkündür; eyni adlar `--clobber` ilə əvəz olunur. .NET/Godot alətləri pinlidir, lakin transitive paketlər, baza image digest-ləri və runner image-ləri tam byte-for-byte reproducible build zəmanəti vermir. CI-də real export nəticəsi olmadan lokal assembly build-ini release export uğuru kimi təqdim etməyin.

Wiki export üçün cari commit-i seçin:

```powershell
pwsh -NoProfile -File tools/publish-wiki.ps1 -SourceRef (git rev-parse HEAD)
```
