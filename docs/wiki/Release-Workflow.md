# Buraxılış və iş axını

## Branch modeli (PRD 122)

```text
feature/*  ─┐
fix/*      ─┼─►  develop  ─►  release/*  ─►  main  ─►  teq + GitHub Release
codex/*    ─┘                                  │
                                               └─►  develop (geri sinxron)
```

| Branch | Rolu | Kim yazır |
|---|---|---|
| `main` | Buraxılmış, sabit kod. Hər commit-i teqlənə bilər. | Yalnız `release/*` və təcili `fix/*` |
| `develop` | İnteqrasiya. Bütün iş burada birləşir. | `feature/*`, `fix/*`, dependabot |
| `feature/*` | Yeni funksionallıq | Töhfə verən |
| `release/*` | Versiya qaldırma, CHANGELOG, son yoxlama | Buraxılışı hazırlayan |

**Qayda:** `main`-ə birbaşa PR açılmır. İş `develop`-a gedir; `develop`-da bütün
yoxlamalar yaşıl olduqda `release/*` vasitəsilə `main`-ə çıxarılır.

> Bu qayda təcrübədən gəlir: əvvəllər feature-lər birbaşa `main`-ə merge olunur,
> sonra `main` geri `develop`-a merge edilirdi. Hər sinxron `develop`-a əlavə
> merge commit-i yazırdı və iki branch-in commit sayı fərqlənirdi — kod eyni
> olsa belə. İndi `develop` həmişə `main`-dən irəlidədir, geri merge lazım deyil.

## Buraxılış addımları

1. `develop`-dan `release/X.Y.Z` yarat.
2. `Directory.Build.props`-da stable/build versiyalarını və tarixi hazırlayın;
   `tools/sync-version.ps1` törəmə istinadları yeniləyir. Tam prosedur:
   [RELEASING](../RELEASING.md).
3. `ProtocolVersion`-a **yalnız** wire format dəyişibsə toxun. Dəyişibsə,
   köhnə client-lər rədd ediləcək — bunu buraxılış qeydlərində yaz.
4. CHANGELOG-da `Buraxılmamış` bölməsini versiya başlığına çevir.
5. Tam yoxlama: `dotnet test`, `tools/e2e-smoke-test.sh`, `tools/check-doc-links.ps1`.
6. `main`-ə və `develop`-a merge et.
7. Teq: **prefikssiz** (`0.3.0`). İlk üç buraxılış `v` prefiksi ilədir; onlar
   tarix olaraq olduğu kimi qalır.
8. `release.yml` yoxlamaların ardından export, SBOM və checksum-ları GitHub Release-ə əlavə edir.
9. **`develop`-u `main`-ə fast-forward et:**

   ```bash
   git push origin origin/main:refs/heads/develop
   ```

   PR merge-i `main`-ə merge commit-i yazır, `develop` isə onu görmür. Bu addım
   olmasa iki branch-in commit sayı hər buraxılışdan sonra bir-bir fərqlənir —
   kod eyni olsa belə. `main` `develop`-u tam ehtiva etdiyi üçün bu
   fast-forward-dur, force-push deyil.

   > Bu push `develop`-un yoxlama tələbini bypass edir və **admin hüququ tələb
   > edir**. Təhlükəsizdir, çünki fast-forward edilən commit artıq `main`-də
   > bütün yoxlamalardan keçib.

> GitHub ən son **yaradılan** release-i "Latest" sayır, ən yüksək versiyanı yox.
> Köhnə buraxılışları sonradan əlavə edirsənsə, sonda `gh release edit <yeni> --latest` çağır.

## Avtomatik yoxlamalar

| Workflow | Nə yoxlayır | Nə vaxt |
|---|---|---|
| `ci.yml` | Build, testlər, Godot layihələri, e2e smoke test, Docker, sənəd linkləri | Hər push və PR |
| `codeql.yml` | C# və Actions üçün statik təhlükəsizlik analizi | Push, PR, həftəlik |
| `dco.yml` | Hər PR commit-in müəllif sign-off-u; mövcud required build job-unda da məcburidir | PR |
| `release.yml` | Yoxlamalardan sonra Windows client/Linux server export, SBOM, checksum, GitHub Release | Teq/release |
| `security.yml` | Zəif/köhnəlmiş paketlər, PR asılılıq nəzarəti, repo gigiyenası | Push, PR, həftəlik |

### Branch protection

| Qayda | `main` | `develop` |
|---|---|---|
| Tələb olunan yoxlamalar | 8 (CodeQL daxil) | 6 |
| Branch güncel olmalıdır (`strict`) | ❌ | ❌ |
| Force push | ❌ | ❌ |
| Branch silinməsi | ❌ | ❌ |
| Admin üçün məcburi | ❌ | ❌ |

Tələb olunan yoxlamalar birbaşa push-u praktiki olaraq bloklayır: push edilən
commit-in keçmiş yoxlaması olmur, ona görə rədd edilir. İş PR ilə gedir.

**`strict` (branch güncel olmalıdır) qəsdən söndürülüb.** Açıq olduqda hər
buraxılış PR-ından əvvəl `develop`-a `main`-dən merge etmək lazım gəlirdi və
bu, iki branch-i hər dəfə bir commit fərqləndirirdi — yəni sənədin əvvəlində
təsvir olunan problemin özünü yaradırdı.

**Approval tələbi də söndürülüb.** Açıq olduqda (0 approval ilə belə) PR
`BLOCKED` vəziyyətində qalırdı və yalnız admin bypass ilə merge olunurdu —
yəni adi töhfə verən üçün qapalı olardı.

**Admin üçün məcburi deyil** — repo sahibi buraxılış sinxronu üçün qaydadan
yan keçə bilir. Bu, şüurlu güzəştdir: tək saxlayıcılı layihədə tam kilid
özünü kilidləmək riskini yaradır. Komanda böyüyəndə `enforce_admins` açılmalıdır.

### Repo təhlükəsizlik parametrləri

- Secret scanning + **push protection** (gizli açar commit edilməsinə mane olur)
- Dependabot security updates
- Merge sonrası branch avtomatik silinir

## Commit qaydaları

- Conventional Commits: `feat(server):`, `fix(client):`, `docs(wiki):`, `chore(deps):`
- **DCO məcburidir:** `git commit -s` ([CONTRIBUTING](../../CONTRIBUTING.md))
- SSH imzası tövsiyə olunur: `tools/setup-signing.ps1`

[Töhfə vermək](../../CONTRIBUTING.md) · [Təhlükəsizlik](../../SECURITY.md) · [Testlər](Testing.md)
