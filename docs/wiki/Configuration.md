# Konfiqurasiya arayışı

## Hansı mənbə həqiqətən işləyir?

| Mənbə | Runtime rolu |
|---|---|
| config/weapons/*.json | Server startup-da WeaponCatalog tərəfindən oxunur |
| config/server_default.json | Hazırda runtime loader yoxdur |
| GameConstants | Versiya, tick, limit, round/economy default-ları |
| client/server project.godot | Engine physics/render/input ayarları |
| Directory.Build.props | Assembly/product build versiyası və C# standartları |
| appsettings.json | ASP.NET konfiqurasiya skeleti |
| infrastructure/.env | Compose dəyişənləri; commit olunmamalıdır |
| localization/*.json | Resurslar mövcuddur, UI inteqrasiyası yoxdur |

Server JSON-dakı friendlyFire=false, password, maxPlayers və tickrate dəyərlərini hazır işləyən operator seçimi kimi qəbul etməyin.

## Silah yükləmə

GameWorld bu yolları sıra ilə sınayır:

1. res://../config/weapons — development repo layout-u.
2. user://config/weapons.
3. AppContext.BaseDirectory/config/weapons.

İlk boş olmayan kataloq seçilir. Sonra weapon_rifle_01 axtarılır. Fayllar top-level *.json olaraq oxunur; subdirectory traversal yoxdur. ID lookup case-insensitive-dir, təkrar ID overwrite edə bilər.

WeaponCatalog JSON property adlarında case-insensitive, comment və trailing comma dəstəyi ilə deserialize edir. Lakin range, fireRate və ammo üçün tam schema/semantic validasiya yoxdur; config dəyişikliklərini test edin.

## Aktiv tüfəng nümunəsi

```json
{
  "id": "weapon_rifle_01",
  "name": "AR-9 Qartal",
  "category": "Rifle",
  "damage": 34,
  "armorPenetration": 0.7,
  "fireRate": 600,
  "automatic": true,
  "magazineSize": 30,
  "reserveAmmo": 90,
  "reloadTime": 2.4,
  "range": 32,
  "damageFalloff": 0.012
}
```

Bu, izahlı qısaldılmış nümunədir; mövcud JSON-u bununla əvəz edib recoilPattern və digər sahələri itirməyin. Tam mənbə [weapon_rifle_01.json](../../config/weapons/weapon_rifle_01.json)-dır.

## Dəyişiklik proseduru

Faylı redaktə edin, JSON-un oxunduğunu və dəyərlərin məntiqli olduğunu yoxlayın, shared testləri çalışdırın, serveri restart edin. Sonra real ammo/cadence/damage davranışını sınaqdan keçirin. Hot reload və client content hash yoxlaması yoxdur.

## Environment dəyişənləri

GODOT_BIN .sh alətlərinə executable yolu verir. E2E-də PORT, SERVER_SECONDS, CLIENT_SECONDS dəyişənləri var. play.sh portu 27015 sabit götürür.

Compose POSTGRES_PASSWORD tələb edir. ConnectionStrings__Postgres və ConnectionStrings__Redis backend container-ə ötürülür, amma bu build-in Program.cs faylı onlardan DB/cache connection yaratmır.

[Silahlar](Weapons.md) · [Operator təlimatı](Operations.md)
