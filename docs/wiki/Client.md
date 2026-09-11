# Godot client

## Başlanğıc

project.godot əsas səhnə kimi Scenes/Main.tscn göstərir. GameBootstrap WorldBuilder vasitəsilə blockout görünüşünü qurur, CanvasLayer altında HUD yaradır, NetworkClient-i qoşur və server handshake nəticəsini gözləyir.

HUD-un CanvasLayer altında olması vacibdir: 3D valideyn altında Control anchor-ları ekran ölçüsünə görə düzgün yerləşməyə bilər. Əvvəlki görünməyən HUD düzəlişi bu strukturla əlaqəlidir.

## Komponentlər

| Sinif | Məsuliyyət |
|---|---|
| GameBootstrap | Səhnə quruluşu, network event-lərinin UI/world-a yönləndirilməsi |
| WorldBuilder | Blockout mesh, material, işıq və görünüş |
| NetworkClient | ENet bağlantısı, input codec, server mesajları |
| LocalPlayerController | Mouse, hərəkət, prediction, reconciliation, kamera |
| RemotePlayerView | Snapshot tarixçəsi və remote interpolation |
| PrototypeHud | Can/armor/ping/ammo/status/crosshair |
| ViewModel | Prosedural silah, bob, sway, kick və reload hərəkəti |
| ShotEffects | Tracer və impact görüntüsü |

## Hadisə axını

HandshakeAccepted lokal oyunçunu yaradır. SnapshotReceived lokal HP/armor və reconciliation üçün istifadə edilir; digər peer-lər üçün RemotePlayerView yaradılır/yenilənir. Snapshot-da olmayan peer modelinin scene node-u silinir.

ShotFired lokal atıcı üçün kamera kick-i və lülədən başlayan tracer yaradır. Remote atış server origin nöqtəsindən göstərilir. DamageReceived shooter üçün hit/kill marker göstərir. WeaponStateReceived ammo HUD və reload animasiyasını yeniləyir.

## Lokal prediction sərhədi

Client serverə input göndərdikdən sonra eyni input-u özünə tətbiq edir və maksimum 128 təsdiqlənməmiş input saxlayır. Snapshot ackSeq ilə keçmiş input-lar təmizlənir. Engine kolliziyası hər iki tərəfdə olduğundan bu tam bit-identical fizika zəmanəti deyil.

## Debug arqumentləri

| Arqument | İstifadə |
|---|---|
| --server ADDRESS | Default 127.0.0.1 |
| --port NUMBER | Default 27015 |
| --name NAME | Görünən username |
| --smoke-test-seconds N | Handshake/snapshot nəticəsi ilə vaxtlı çıxış |
| --auto-fire true | Test üçün avtomatik tətik |
| --screenshot PATH | Render edilən viewport-u təxminən 2 saniyə sonra PNG saxlamaq |

Parser flag-dan sonrakı dəyəri axtarır, ona görə --auto-fire dəyərsiz son arqument kimi verilməməlidir. Screenshot headless rejim üçün nəzərdə tutulmayıb.

## Hazır olmayan UI

Tam main menu, server browser, login, scoreboard, settings, buy/inventory və chat ekranları yoxdur. Localization JSON-ları UI-yə bağlanmayıb; bəzi status mətnləri kodda sabitdir.

[Mənbə](../../client/Scripts/Core/GameBootstrap.cs) · [UI və audio](UI-Audio-and-Localization.md)
