# Dəyişiklik jurnalı

Bu jurnal tətbiq edilmiş dəyişiklikləri məhsulun gələcək planlarından ayırır. Git teqi `0.2.2`, proqram/content versiyası `0.2.2`-dır. Əvvəlki buraxılışların teqlərində `v` prefiksi var; bu buraxılışın teqi istifadəçinin istədiyi kimi `0.2.2`-dir. Bu, prototipin production-ready olması demək deyil.

## Buraxılmamış

### Əlavə edildi

- Prosedural audio sintezi: atəş, dry-fire, reload, impact, hit/kill marker və 8 səth üçün addım səsi. Layihədə audio faylı yoxdur — hamısı kodda yaradılır (PRD 143, 144).
- `FootstepTracker`: məsafə əsaslı addım kadensiyası; Shift ilə addımlamaq və çömbəlmək səssizdir (PRD 12, 83).
- `BlockoutMap.SurfaceAt()`: ayaq altındakı səth materialı (PRD 84).
- Spatial audio: başqa oyunçuların atəşi, addımı və reload-u mövqedən səslənir; 24 elementlik oxuducu hovuzu.
- `--dump-audio` debug flaqı ilə səslərin WAV-a yazılması və yoxlanması.
- Töhfə verənlər üçün DCO tələbi; lisenziyanın arxitektura nəticəsi sənədləşdirildi.

### Dəyişdi

- CI-dəki Wiki export `0.2.2` teqinə deyil, cari commit-ə bağlandı — permalink-lər köhnəlmir.
- Test asılılıqları yeniləndi (xunit 2.9.3, Test.Sdk 18.10.0, runner.visualstudio 4.0.0); GitHub Actions versiyaları artırıldı.

## 0.2.2 — 2026-09-11

### Əlavə edildi

- Serverdə `WeaponRuntime`: atəş intervalı, şarjor, ehtiyat ammo, manual və avtomatik reload.
- Avtomatik/semi-avtomatik silah davranışı və konfiqurasiyada `automatic`.
- Shared deterministik recoil pattern, recovery və spray reset.
- `ShotFired` və `WeaponState` mesajları; atəş effektləri və sahibə ammo yenilənməsi.
- Prosedural view model, muzzle flash, tracer, hit/kill marker, recoil crosshair və ammo HUD.
- Gunplay regression testləri; buraxılışın yekun nəticələri [test hesabatında](docs/RELEASE-0.2.2.md).
- Oyun, proqram təminatı və əməliyyatları əhatə edən geniş Wiki; repo töhfə, dəstək, təhlükəsizlik və asset sənədləri.
- Mənbədən Wiki nəşri və Markdown link yoxlaması üçün PowerShell alətləri.

### Dəyişdi

- Atəş cəhdi hər input paketinə görə cəzalandırılmır; silah server tick-ində bir dəfə işləyir.
- Reload davam edərkən atəş bloklanır; ilk atış sıfır recoil ilə başlayır.
- Oyun/content/build versiyası 0.2.2, protocol versiyası 2 oldu. Köhnə protokol client-ləri rədd edilir.
- README və arxitektura real runtime ilə uyğunlaşdırıldı; roadmap hədəfləri ayrıca göstərildi.

### Məhdudiyyətlər

Divar occlusion-u, tam round/bomb/economy inteqrasiyası, səs, online hesablar və matchmaking tamamlanmayıb. [KNOWN_ISSUES](docs/KNOWN_ISSUES.md).

## v0.2.1

- Dünya/HUD görünüşü və lokal başlanğıc düzəlişləri.
- Server spawn mövqeyi və baxış istiqamətinin client-ə tətbiqi.
- Oynatma skriptləri və kursor tutulması davranışı.

## v0.2.0

- İlk oynanıla bilən şəbəkə prototipi.
- ENet binar protokol, prediction/reconciliation, interpolation.
- Server movement, hitscan, lag compensation, damage və respawn.
- İki client ilə E2E smoke test infrastrukturu.

## v0.1.0

- PRD, ilkin arxitektura və repo skeleti.
- Shared qaydalar, silah JSON-ları, backend registry və CI/Docker başlanğıcı.
