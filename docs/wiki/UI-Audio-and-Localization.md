# UI, audio və localization

## Hazır UI

PrototypeHud status, health, armor, ping, ammo və crosshair verir. GameBootstrap HUD-u CanvasLayer altında yerləşdirir. Bu, ekran koordinatlarını 3D world transform-dan ayırır və viewport ölçüsünə uyğun Control yerləşməsini təmin edir.

Crosshair recoil feedback-i və server təsdiqli hit/kill marker göstərə bilir. Ammo şarjor/reserve və reload statusuna əsaslanır. Server event itəndə UI köhnələ bilər; HUD authoritative gameplay state deyil.

## Silah görüntüsü

ViewModel mesh-ləri prosedural qurulur. Bob/sway, atış kick-i, aşağı-yuxarı reload hərəkəti və muzzle flash prototip hissi verir. Bunlar final artist rig/animasiya pipeline-ı deyil.

ShotEffects tracer və impact görüntüsü yaradır. Lokal atıcıda tracer silah lüləsindən başlayır, nəticə nöqtəsi server hadisəsindən gəlir. Bu görüntü divar penetration sisteminin həyata keçirildiyini göstərmir.

## Audio vəziyyəti

Atəş, reload, impact, hit marker və material əsaslı footstep səsləri işləyir. Voice chat (PRD 64), qumbara, bomba və qapı səsləri hələ yoxdur.

**Səslər kodda sintez olunur — layihədə audio faylı yoxdur.** `ProceduralAudio` küy, sinus, eksponensial zərf və birpolyus filtrlərdən istifadə edərək `AudioStreamWav` yaradır. Səbəb hüquqidir: PRD 143 orijinal audio tələb edir, PRD 144 başqa oyunların səslərini qadağan edir. Sintez edilmiş səs təbiətcə orijinaldır. Bu, final səs dizaynı deyil.

| Səs | Kanal | Məsafə |
|---|---|---|
| Öz atəşin, hit marker, öz reload-un | 2D (`AudioStreamPlayer`) | — |
| Başqasının atəşi | 3D | 70 m |
| Güllə impact-ı | 3D | 70 m |
| Addım, başqasının reload-u | 3D | 24 m |

3D oxuducular 24 elementlik hovuzdan verilir; spray zamanı hər atəş üçün node yaratmaq GC yükü yaradardı.

### Footstep məntiqi

Addımlar taymerlə deyil, qət edilmiş məsafə ilə ölçülür (`FootstepTracker`, stride 1.9 m). Shift ilə addımlayan və çömbəlmiş oyunçu **səssizdir** — bu, taktiki FPS-in təməl mexanikasıdır. Addım mənbəyi server-authoritative mövqelərdir, ona görə oyunçu öz addım səsini başqasının client-ində gizlədə bilmir.

Səth materialı `BlockoutMap.SurfaceAt()` ilə ayağın altındakı blokdan götürülür (PRD 84). Ölçülmüş parlaqlıq aralığı 568 Hz (taxta) — 7612 Hz (beton), yəni materiallar qulaqla fərqlənir.

### Yoxlama

`--dump-audio <qovluq>` sintez edilmiş səsləri WAV kimi yazır. Bu, oyunu işə salmadan dalğa formasını analiz etməyə imkan verir: səssizlik, klipinq və materialların fərqliliyi bu yolla yoxlanılıb.

Hələ sənədləşdirilməli olanlar: eyni vaxtlı voice limiti, volume kateqoriyaları (PRD 80) və occlusion.

## Dillər

localization/az.json, en.json, ru.json mövcuddur. Godot fallback locale az olaraq qurulub, lakin həmin JSON-ları oxuyan runtime localization service yoxdur. HUD-da kod içi mətnlər qalır.

Tərcümə əlavə etmək üçün eyni açarı bütün dil fayllarında saxlayın; sonra həmin açarı UI-dən oxuyan inteqrasiyanı qurun. Uzun mətn, Azərbaycan hərfləri və rus kirilinin layout/font dəstəyini real render ilə yoxlayın.

## Accessibility və settings

Tam remap, FOV slider, sensitivity, audio categories, contrast preset və digər accessibility controls hazır deyil. project.godot input adları istifadəçi üçün settings ekranı demək deyil.

Render acceptance üçün müxtəlif viewport ölçülərində HUD sərhədləri, oxunaqlıq, kursor capture/release və recoil zamanı crosshair görünüşü sınaqdan keçirilməlidir. Headless smoke testi bunları ölçmür.

[Client](Client.md) · [Asset qaydaları](../../ASSETS.md) · [Testlər](Testing.md)
