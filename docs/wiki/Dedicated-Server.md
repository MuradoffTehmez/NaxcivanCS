# Dedicated server

## Başlatmaq

Repo kökündən Godot .NET executable-ı ilə:

```bash
godot --headless --path server -- --port 27015 --map NC_Qala
```

PATH-də godot yoxdursa tam executable yolu verin və ya `bash tools/run-server.sh` istifadə edin. Script əvvəl server assembly-sini build edir. --map hazırda yalnız log adı rolundadır; blockout eynidir.

## Lifecycle

ServerMain əvvəl BlockoutMap kolliziyasını qurur, GameWorld yaradır, sonra NetworkServer-i əlavə edib portu dinləməyə başlayır. Port açıla bilmirsə çıxış kodu 1 olur.

GameWorld config/weapons kataloqunu yükləyir və weapon_rifle_01 axtarır. Kataloq tapılmasa error yazılır; bu vəziyyət serverin işlək gunplay verməsi kimi qəbul edilməməlidir.

## Tick dövrəsi

```text
Input növbəsi → MovementSimulation → MoveAndSlide
            → WeaponRuntime → HitValidator → Damage
            → History.Record → Respawn yoxlaması
Hər 2 tick → WorldSnapshot
```

Server physics 64 Hz, snapshot 32 Hz hədəfləyir. Bunlar konfiqurasiya edilmiş tezliklərdir, hər yükdə ölçülmüş sabitlik zəmanəti deyil.

## Qoşulma

ENet CreateServer maksimum 10 peer üçün qurulur. Handshake protocol 2 və trim-dən sonra 3–20 simvolluq username yoxlayır. Komanda `oyunçu sayı % 2` ilə seçilir; tam balans/party/team selection sistemi yoxdur.

## Oyunçu vəziyyəti

ServerPlayer health/armor, movement, weapon runtime, latency, history və input növbəsi saxlayır. Növbə maksimum 32 input saxlayır, hər tick ən çox 4 input emal edir. Köhnə sequence üçün baza yoxlama mövcuddur; tam packet reordering/flood hardening ayrıca işdir.

Death zamanı velocity sıfırlanır. Təxminən 3000 ms sonra health və silah ammo-su bərpa edilir. Round sistemi olmadığından bu respawn qaydası oyunun əsas test dövrəsidir.

## Şəbəkə çıxışı

Snapshot, shot və weapon state unreliable gedir. Damage və handshake reliable-dir. Ammo yalnız sahibə göndərilir, amma hazırda spawn/respawn üçün ayrıca state event yoxdur. Disconnect zamanı player/body silinir.

## Backend və idarəetmə

Server özü backend heartbeat göndərmir. CLI port/map/smoke-test-seconds-dən başqa geniş admin parametrləri təqdim etmir. server_default.json password/friendlyFire/tickrate kimi sahələr runtime-da tətbiq edilmir.

--smoke-test-seconds N prosesi vaxtlı dayandırır; bu, health endpoint deyil. Public server üçün authentication, validation, resource limit, logging və monitorinq işi ayrıca tamamlanmalıdır.

[Mənbə](../../server/Scripts/ServerCore/ServerMain.cs) · [Konfiqurasiya](Configuration.md) · [Əməliyyatlar](Operations.md)
