# UI, audio və localization

## Hazır UI

PrototypeHud status, health, armor, ping, ammo və crosshair verir. GameBootstrap HUD-u CanvasLayer altında yerləşdirir. Bu, ekran koordinatlarını 3D world transform-dan ayırır və viewport ölçüsünə uyğun Control yerləşməsini təmin edir.

Crosshair recoil feedback-i və server təsdiqli hit/kill marker göstərə bilir. Ammo şarjor/reserve və reload statusuna əsaslanır. Server event itəndə UI köhnələ bilər; HUD authoritative gameplay state deyil.

## Silah görüntüsü

ViewModel mesh-ləri prosedural qurulur. Bob/sway, atış kick-i, aşağı-yuxarı reload hərəkəti və muzzle flash prototip hissi verir. Bunlar final artist rig/animasiya pipeline-ı deyil.

ShotEffects tracer və impact görüntüsü yaradır. Lokal atıcıda tracer silah lüləsindən başlayır, nəticə nöqtəsi server hadisəsindən gəlir. Bu görüntü divar penetration sisteminin həyata keçirildiyini göstərmir.

## Audio vəziyyəti

Tam shot, reload, footstep, hit, voice və material audio sistemi yoxdur. Audio qovluğunun olması işlək səs pipeline-ı demək deyil. Səs gəlməməsi bu release-də avtomatik cihaz problemi sayılmamalıdır.

Yeni audio üçün asset mənbəyi, playback məsafəsi, spatial/2D seçimi, eyni vaxtlı voice limiti və volume kateqoriyası sənədləşdirilməlidir.

## Dillər

localization/az.json, en.json, ru.json mövcuddur. Godot fallback locale az olaraq qurulub, lakin həmin JSON-ları oxuyan runtime localization service yoxdur. HUD-da kod içi mətnlər qalır.

Tərcümə əlavə etmək üçün eyni açarı bütün dil fayllarında saxlayın; sonra həmin açarı UI-dən oxuyan inteqrasiyanı qurun. Uzun mətn, Azərbaycan hərfləri və rus kirilinin layout/font dəstəyini real render ilə yoxlayın.

## Accessibility və settings

Tam remap, FOV slider, sensitivity, audio categories, contrast preset və digər accessibility controls hazır deyil. project.godot input adları istifadəçi üçün settings ekranı demək deyil.

Render acceptance üçün müxtəlif viewport ölçülərində HUD sərhədləri, oxunaqlıq, kursor capture/release və recoil zamanı crosshair görünüşü sınaqdan keçirilməlidir. Headless smoke testi bunları ölçmür.

[Client](Client.md) · [Asset qaydaları](../../ASSETS.md) · [Testlər](Testing.md)
