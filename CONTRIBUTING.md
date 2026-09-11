# Töhfə vermək

NaxcivanCS prototipdir. Dəyişikliyə başlamazdan əvvəl [mövcud vəziyyəti](docs/PROJECT_ANALYSIS.md), [Wiki-ni](docs/wiki/Home.md) və [məlum məhdudiyyətləri](docs/KNOWN_ISSUES.md) oxuyun.

## İş axını

1. Mövcud issue-larda eyni problemi axtarın və gözlənilən davranışı yazın.
2. Aktual develop-dan mövzu budağı yaradın. Feature/fix adları tarixi workflow-dur; Codex dəyişiklikləri `codex/` prefiksi istifadə edə bilər.
3. Dəyişikliyi kiçik, məqsədi aydın commit-lərə bölün.
4. Uyğun build və testləri icra edin.
5. PR-də konkret problem, nəticə və yoxlama sübutunu göstərin.
6. CI keçdikdən sonra inteqrasiya edin. Main buraxılışların bazasıdır.

```bash
git fetch origin
git switch develop
git pull --ff-only
git switch -c feature/short-description
```

Solo development ayrıca məcburi reviewer tələb etməyə bilər, lakin CI nəticələri yoxlanmalıdır. Branch protection varsa onu keçməyin.

### Branch qaydası

İş `develop`-a gedir, `main`-ə yox. `main` yalnız `release/*` qəbul edir.

```bash
git checkout develop && git pull
git checkout -b feature/qisa-ad
# ... iş ...
git commit -s -m "feat(server): ..."
gh pr create --base develop
```

`main`-ə birbaşa PR açsanız, branch protection onu bloklayacaq.
Tam axın: [Release Workflow](docs/wiki/Release-Workflow.md).

## Kod standartları

Nullable və implicit usings aktivdir. Sinif/metod/property üçün PascalCase, lokal dəyişən üçün camelCase, private field üçün _camelCase işlədin. Faylın mövcud üslubunu saxlayın. Shared qatına Godot tipi əlavə etməyin. Client-dən health, kill, money və rank təyin edən mesaj qəbul etməyin.

Balans üçün JSON mənbəsinə üstünlük verin, amma yeni sahənin runtime-da həqiqətən oxunduğunu sübut edin. Localization faylına açar əlavə etmək kifayət deyil: UI-nin onu oxuması da tələb olunur. Hələ qoşulmayan sahələri hazır funksiya kimi sənədləşdirməyin.

## Yoxlama

```bash
dotnet build NaxcivanCS.sln -c Release
dotnet test NaxcivanCS.sln -c Release --no-build --collect:"XPlat Code Coverage" --settings coverage.runsettings --results-directory TestResults
dotnet format NaxcivanCS.sln --verify-no-changes --no-restore
pwsh -NoProfile -File tools/check-coverage.ps1
dotnet build server/NaxcivanCS.Server.csproj
dotnet build client/NaxcivanCS.Client.csproj
bash tools/e2e-smoke-test.sh
```

E2E üçün `GODOT_BIN` təyin edin. Protokol dəyişirsə hər iki tərəfi yeniləyin, versiya qapısını və codec testlərini yoxlayın. Gameplay üçün [əl ilə test ssenarilərini](docs/TESTING.md) də yerinə yetirin.

## Commit və PR

Conventional Commits nümunələri: `feat(server): ...`, `fix(client): ...`, `docs(wiki): ...`, `test(shared): ...`. PR-də nə dəyişdiyini və necə yoxlandığını yazın; nəticəsi olmayan testləri keçmiş kimi göstərməyin.

Sənəd dəyişikliklərində `docs/wiki/` mənbəyini redaktə edin, lokal linkləri yoxlayın və buraxılışdan sonra Wiki-ni nəşr edin. PRD məhsul hədəfidir; hər PR ilə onun bütün hədəflərini tamamlanmış göstərməyin.

## Mənşə sertifikatı (DCO)

Yeni `.cs` faylı əlavə edərkən SPDX başlığı tələb olunur; `dotnet format` onu
[.editorconfig](.editorconfig)-dəki şablondan avtomatik yazır, CI isə header-siz faylı rədd edir:

```csharp
// SPDX-FileCopyrightText: 2026 Tahmaz Muradov
// SPDX-License-Identifier: GPL-3.0-or-later
```

Layihə **GPL-3.0-or-later** altındadır. Töhfə verən öz kodunun müəllif hüququnu saxlayır,
ona görə lisenziyanı sonradan dəyişmək üçün hər töhfə verənin razılığı lazım olacaq.
Bunu idarə oluna bilən saxlamaq üçün hər commit [Developer Certificate of
Origin 1.1](https://developercertificate.org/) ilə imzalanmalıdır.

Commit-ə `Signed-off-by` sətri əlavə etmək üçün:

```bash
git commit -s -m "feat(server): ..."
```

Bu sətir o deməkdir ki, göndərdiyiniz kodu ya özünüz yazmısınız, ya da onu
layihənin lisenziyası altında təqdim etmək hüququnuz var. Mənbəyi bilinməyən
və ya uyğun olmayan lisenziyalı kod qəbul edilmir.

DCO avtomatik yoxlanır: `Signed-off-by: Ad Soyad <email>` commit müəllifinin adı/email-i ilə uyğun olmalıdır. PR-dakı bütün **məzmun** commit-ləri yoxlanır; yalnız son commit-ə sign-off əlavə etmək kifayət deyil. Merge commit-ləri yoxlamadan azaddır — GitHub-un merge düyməsi onları imzalaya bilmir və özləri yeni müəllif məzmunu gətirmir; merge-in gətirdiyi hər commit isə ayrıca yoxlanır, ona görə imzasız kod merge arxasında gizlənə bilmir. Standalone `DCO sign-off` ilə yanaşı artıq tələb olunan `Shared + Backend (build, test)` job-u da bu yoxlamanı icra edir; reviewer sayı artırılmır.

Yalnız öz commit-inizdə unudulmuş sign-off üçün `git commit --amend --no-edit -s` istifadə edin. Başqasının adından sign-off yaratmayın. Müəllif `git config user.name` və `user.email` dəyərlərini düzgün təyin etməlidir. Tarixçə yenidən yazılarkən mövcud branch/force-push qaydalarına əməl edin.

Lokal yoxlama: `pwsh -NoProfile -File tools/check-dco.ps1 -BaseSha <tam-base-SHA> -HeadSha <tam-head-SHA>`.

SSH ilə commit imzalamaq üçün: `tools/setup-signing.ps1`.

## Material və təhlükəsizlik

Asset-in mənşə və icazəsini [ASSETS.md](ASSETS.md) qaydasına uyğun qeyd edin. Parol, token, .env, şəxsi oyunçu məlumatları və lokal log arxivlərini commit etməyin. Təhlükəsizlik problemi üçün [SECURITY.md](SECURITY.md), ünsiyyət üçün [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) tətbiq olunur.
