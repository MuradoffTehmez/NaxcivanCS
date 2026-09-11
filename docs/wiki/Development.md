# Proqramçı üçün development workflow

## Kod xəritəsi

| Dəyişiklik | Başlanğıc nöqtəsi |
|---|---|
| Movement və prediction | shared/Gameplay/MovementSimulation, client/Player/LocalPlayerController |
| Atəş vaxtı/ammo/reload | shared/Gameplay/WeaponRuntime |
| Recoil | shared/Gameplay/RecoilCalculator və config/weapons |
| Server simulyasiyası | server/ServerCore/GameWorld |
| Hit registration | server/Damage/HitValidator, shared/Gameplay/HitScan |
| Wire format | shared/Net/PacketCodec, SnapshotSerializer |
| Vizual feedback | client/Weapons/ViewModel, client/Effects/ShotEffects |
| HUD | client/UI/PrototypeHud |
| API | backend/.../Endpoints |
| Test | tests/NaxcivanCS.Shared.Tests |

Yollar cədvəldə qısaldılıb; tam repo strukturu [README](../../README.md)-dədir.

## Budaq və lokal dövrə

Aktual develop-dan iş budağı yaradın. Server/client arasında yayılan dəyişiklikləri eyni PR-də saxlayın. Shared net8.0 qatı Godot-suz build olunmalıdır; engine Node, Vector3 və scene tiplərini ora gətirməyin.

```bash
dotnet build NaxcivanCS.sln -c Release
dotnet test NaxcivanCS.sln -c Release --no-build
dotnet build server/NaxcivanCS.Server.csproj
dotnet build client/NaxcivanCS.Client.csproj
```

Solution build Godot assembly-lərinin uğurunu göstərmir. `--no-build` testini mənbə dəyişdikdən sonra köhnə binary üzərində işlətməyin.

## Gameplay dəyişikliyi

Əvvəl qaydanı shared unit testdə ifadə edin, sonra GameWorld/Network qatına inteqrasiya edin. Client-ə nəticəni hansı mesajın daşıdığını müəyyənləşdirin. İlk qoşulma, respawn və paket itkisi üçün bərpa yolunu ayrıca düşünün: yalnız bir hadisənin çatmasına bağlanan UI köhnələ bilər.

## Protokol dəyişikliyi

MessageType ID-lərini təkrar istifadə etməyin. Encoder/decoder, payload ölçüsü, client handler, server handler və uyğunluq qapısı birlikdə dəyişməlidir. ProtocolVersion artımı köhnə client-in səssiz uyğunsuz işləməsinin qarşısını alır.

## Config və sənəd

Yeni JSON sahəsi üçün onu hansı runtime metodunun oxuduğunu göstərin. “Config var” ilə “config işləyir” fərqlidir. İctimai davranış dəyişəndə Wiki, CHANGELOG və KNOWN_ISSUES eyni PR-də yenilənməlidir.

Kod formatında mövcud .editorconfig və nullable qaydaları saxlanır. CI format yoxlaması hazırda continue-on-error olduğundan format xətası ayrıca nəzərdən keçirilməlidir.

[CONTRIBUTING](../../CONTRIBUTING.md) · [Testlər](Testing.md) · [Release](Release-Workflow.md)
