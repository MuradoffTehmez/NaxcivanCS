# ENet wire protokolu

## Ümumi format

ProtocolVersion **2**-dir. Paketlər little-endian binar formatdadır. İlk 2 bayt ushort MessageType, qalan hissə payload-dır. Aşağıdakı ölçülər ENet/UDP/IP overhead-i daxil etmir.

| ID | Mesaj | İstiqamət | Çatdırılma | Payload |
|---|---|---|---|---:|
| 1 | Handshake | C→S | Reliable | 5+N |
| 2 | InputCommand | C→S | Unreliable | 32 |
| 6 | Disconnect | C→S | Handler mövcuddur | ayrıca codec yoxdur |
| 100 | HandshakeAccepted | S→C | Reliable | 13 |
| 101 | HandshakeRejected | S→C | Reliable | UTF-8 səbəb |
| 102 | WorldSnapshot | S→C | Unreliable | 14+32N |
| 104/105 | PlayerDamaged/Killed | S→C | Reliable | 12 |
| 110 | ShotFired | S→C | Unreliable | 39 |
| 111 | WeaponState | S→sahib | Unreliable | 5 |

BuyRequest, ChatMessage, TeamSelect və round/bomb/match enum dəyərləri gələcək üçündür; enum-da olmaq işlək handler demək deyil.

## Handshake

```text
protocolVersion: i32
usernameLength: u8
username: UTF-8 bytes
```

Encoder username üçün maksimum 64 bayt tətbiq edir. Server decode-dan sonra trim və 3–20 simvol yoxlaması edir. Accepted payload peerId:i32, team:u8, serverTimeMs:f64-dür.

Rədd səbəbləri version_mismatch, invalid_username, server_not_ready və malformed_packet ola bilər. Handshake hesab authentication-ı deyil.

## InputCommand

| Offset | Sahə | Tip |
|---:|---|---|
| 0 | Sequence | u32 |
| 4 | ClientTimeMs | f64 |
| 12 | MoveForward | f32 |
| 16 | MoveRight | f32 |
| 20 | YawDegrees | f32 |
| 24 | PitchDegrees | f32 |
| 28 | Buttons | u32 |

Tam input paketi 34 baytdır. Move oxları [-1,1] aralığına clamp edilir, NaN ox sıfır olur. Bu, bütün float sahələrinin sonlu qiymət validasiyası demək deyil.

Buttons bitləri: Jump 0, Crouch 1, Walk 2, PrimaryFire 3, SecondaryFire 4, Reload 5, Interact 6, Drop 7, BuyMenu 8, Scoreboard 9. Cari client bunların yalnız bir hissəsini real girişdən toplayır.

## Snapshot

Header tick:u32, serverTimeMs:f64, count:u16 — 14 bayt.

| Player offset | Sahə | Bayt |
|---:|---|---:|
| 0 | peerId i32 | 4 |
| 4 | position x/y/z f32 | 12 |
| 16 | yaw f32 | 4 |
| 20 | pitch f32 | 4 |
| 24 | health u8 | 1 |
| 25 | armor u8 | 1 |
| 26 | flags u8, bit 0=crouching | 1 |
| 27 | team u8 | 1 |
| 28 | acknowledged sequence u32 | 4 |

10 player üçün 334 payload, 336 application packet baytı alınır. Snapshot velocity, ammo, weapon id və grounded state daşımır.

## ShotFired və ammo

ShotFired shooterId:i32, origin:3×f32, end:3×f32, punchPitch:f32, punchYaw:f32, shotIndex:u16, hit:u8 saxlayır. Bu nəticə effekt üçündür; client-dən gələn hit iddiası yoxdur.

WeaponState magazine:u16, reserve:u16, reloading:u8-dir. Hadisə əsasında unreliable yayım paket itkisinə qarşı periodik bərpa vermir.

## Damage

victim:i32, attacker:i32, hitBox:u8, healthDamage:u16, killed:u8. Lethal nəticədə MessageType=105 seçilir.

## Uyğunluq və test

Protocol 1 bu build-ə qoşula bilməz. Game/content string-ləri handshake-də müqayisə edilmir. Payload dəyişəndə codec round-trip, truncation, ölçü və cross-version qəbul testlərini yeniləyin.

[Mənbə: PacketCodec](../../shared/NaxcivanCS.Shared/Net/PacketCodec.cs) · [SnapshotSerializer](../../shared/NaxcivanCS.Shared/Net/SnapshotSerializer.cs)
