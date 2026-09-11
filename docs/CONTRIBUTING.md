# Töhfə qaydaları

## Git workflow (PRD 122)

```text
main       — yalnız release-lər, həmişə deploy edilə bilən
develop    — inteqrasiya branch-i
feature/*  — yeni funksionallıq
fix/*      — düzəlişlər
release/*  — release hazırlığı
```

Axın: `Issue → Branch → Commit → Pull Request → CI → Review → Merge`

Solo development zamanı mandatory approval tələb edilməyə bilər, **lakin CI keçməlidir.**

## Commit mesajları

Conventional Commits:

```text
feat(economy): loss bonus pillesini config-den oxu
fix(network): handshake protocol version yoxlamasi
docs(prd): map callout bolmesini genislendir
chore(ci): dotnet 8 pin
test(damage): helmet headshot senarisi
```

Scope-lar: `client`, `server`, `backend`, `shared`, `config`, `infra`, `ci`, `docs`.

## Kod standartları (PRD 154)

```text
PascalCase   → class, method, property
camelCase    → lokal dəyişən, parametr
_privateField → private sahə
IInterface   → interface
```

- Nullable reference types **aktiv** (`Directory.Build.props`)
- Warning-lər CI-də nəzarətdədir — yeni warning əlavə etmə
- Fayl başına bir əsas tip

## Toxunulmaz qaydalar

1. **PRD 156 — client-ə səlahiyyət verilmir.** `setHealth`, `giveMoney`, `giveWeapon`,
   `setRank`, `registerKill`, `awardXP` heç vaxt client-dən gəlməməlidir.
2. **PRD 155 — balans rəqəmi kodda hardcode edilmir.** `config/` faylına yaz.
3. **PRD 118 — UI string-i kodda hardcode edilmir.** `localization/*.json`-a yaz.
4. **PRD 144 — Counter-Strike/Valve asset, audio, model, xəritə və ya kodu istifadə edilmir.**
   İlham almaq olar, surət çıxarmaq olmaz.
5. **PRD 23 — visual-only smoke istifadə edilmir.** Smoke server tərəfdə yaradılır.

## Lokal yoxlama

```bash
dotnet build NaxcivanCS.sln && dotnet test NaxcivanCS.sln
```
