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

## Kod standartları

Nullable və implicit usings aktivdir. Sinif/metod/property üçün PascalCase, lokal dəyişən üçün camelCase, private field üçün _camelCase işlədin. Faylın mövcud üslubunu saxlayın. Shared qatına Godot tipi əlavə etməyin. Client-dən health, kill, money və rank təyin edən mesaj qəbul etməyin.

Balans üçün JSON mənbəsinə üstünlük verin, amma yeni sahənin runtime-da həqiqətən oxunduğunu sübut edin. Localization faylına açar əlavə etmək kifayət deyil: UI-nin onu oxuması da tələb olunur. Hələ qoşulmayan sahələri hazır funksiya kimi sənədləşdirməyin.

## Yoxlama

```bash
dotnet build NaxcivanCS.sln -c Release
dotnet test NaxcivanCS.sln -c Release --no-build
dotnet build server/NaxcivanCS.Server.csproj
dotnet build client/NaxcivanCS.Client.csproj
bash tools/e2e-smoke-test.sh
```

E2E üçün `GODOT_BIN` təyin edin. Protokol dəyişirsə hər iki tərəfi yeniləyin, versiya qapısını və codec testlərini yoxlayın. Gameplay üçün [əl ilə test ssenarilərini](docs/TESTING.md) də yerinə yetirin.

## Commit və PR

Conventional Commits nümunələri: `feat(server): ...`, `fix(client): ...`, `docs(wiki): ...`, `test(shared): ...`. PR-də nə dəyişdiyini və necə yoxlandığını yazın; nəticəsi olmayan testləri keçmiş kimi göstərməyin.

Sənəd dəyişikliklərində `docs/wiki/` mənbəyini redaktə edin, lokal linkləri yoxlayın və buraxılışdan sonra Wiki-ni nəşr edin. PRD məhsul hədəfidir; hər PR ilə onun bütün hədəflərini tamamlanmış göstərməyin.

## Material və təhlükəsizlik

Asset-in mənşə və icazəsini [ASSETS.md](ASSETS.md) qaydasına uyğun qeyd edin. Parol, token, .env, şəxsi oyunçu məlumatları və lokal log arxivlərini commit etməyin. Təhlükəsizlik problemi üçün [SECURITY.md](SECURITY.md), ünsiyyət üçün [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) tətbiq olunur.
