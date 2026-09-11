# NaxcivanCS
## Product Requirements Document — PRD

**Layihə adı:** NaxcivanCS  
**Məhsul tipi:** Competitive Multiplayer Tactical FPS  
**Janr:** First-Person Shooter / Tactical Shooter  
**Əsas platforma:** Windows PC  
**Gələcək platformalar:** Linux, macOS — uyğunluq araşdırıldıqdan sonra  
**Oyun mühərriki:** Godot 4 + C#  
**Multiplayer modeli:** Dedicated Server / Server Authoritative  
**Əsas dil:** Azərbaycan dili  
**Əlavə dillər:** English, Русский  
**İlkin oyunçu sayı:** 5v5  
**Əsas oyun rejimi:** Bomb / Defuse  
**Status:** Pre-production  
**Sənəd versiyası:** 1.0

---

# 1. Məhsulun təsviri

NaxcivanCS sürətli, rəqabətli və aşağı giriş baryerinə malik multiplayer tactical FPS oyunudur.

Oyun klassik komanda əsaslı FPS-lərin əsas üstünlüklərindən ilhamlanacaq:

- round-based gameplay;
- iki komanda;
- iqtisadiyyat sistemi;
- silah alma sistemi;
- bomba yerləşdirmə və zərərsizləşdirmə;
- recoil və spray control;
- taktiki xəritələr;
- sürətli reaksiya;
- komanda koordinasiyası;
- individual skill;
- competitive ranking.

Lakin NaxcivanCS müstəqil məhsul olmalıdır.

Counter-Strike və ya başqa oyunlardan aşağıdakılar kopyalanmamalıdır:

- xəritələr;
- modellər;
- teksturalar;
- səslər;
- animasiyalar;
- UI elementləri;
- ikonlar;
- silah modelləri;
- kod;
- trademark elementləri;
- Valve loqoları;
- Counter-Strike loqosu.

NaxcivanCS öz vizual kimliyinə, xəritələrinə, lore elementlərinə, UI sisteminə və gameplay balansına malik olacaq.

---

# 2. Məhsul vizyonu

NaxcivanCS-in məqsədi aşağı sistem tələbləri olan kompüterlərdə belə işləyə bilən, Azərbaycan oyunçuları üçün lokal xarakter daşıyan, eyni zamanda beynəlxalq competitive FPS bazarında istifadə edilə biləcək multiplayer oyun yaratmaqdır.

Əsas prinsip:

> “Easy to learn, difficult to master.”

Yeni oyunçu oyunu bir neçə dəqiqə ərzində başa düşməli, amma yüksək səviyyədə oynamaq üçün aim, recoil control, movement, positioning, map knowledge və komanda koordinasiyasını inkişaf etdirməlidir.

---

# 3. Məhsulun əsas məqsədləri

NaxcivanCS aşağıdakı xüsusiyyətlərə malik olmalıdır:

1. Stabil multiplayer.
2. Aşağı input latency.
3. Server-authoritative gameplay.
4. Competitive integrity.
5. Skill-based gunplay.
6. Optimallaşdırılmış performans.
7. Azərbaycan tematikası.
8. Original xəritələr.
9. Ranking sistemi.
10. Matchmaking.
11. Anti-cheat infrastrukturu.
12. Statistikalar.
13. Leaderboard.
14. Party sistemi.
15. Dost sistemi.
16. Spectator sistemi.
17. Replay sistemi.
18. Dedicated servers.
19. Admin/moderator sistemi.
20. Cosmetics əsaslı monetizasiya.

---

# 4. Hədəf auditoriya

Əsas auditoriya:

**Yaş:** təxminən 16–35

**Regionlar:**

- Azərbaycan
- Türkiyə
- Gürcüstan
- Qazaxıstan
- Orta Asiya
- Şərqi Avropa

Daha sonra:

- Avropa
- Yaxın Şərq

Oyun aşağı və orta səviyyəli gaming PC istifadəçiləri üçün də əlçatan olmalıdır.

---

# 5. Məhsul prinsipləri

NaxcivanCS üçün əsas dizayn prinsipləri:

### Competitive First

Gameplay balansı vizual effektlərdən daha vacibdir.

### No Pay-to-Win

Real pul qarşılığında:

- daha güclü silah;
- əlavə damage;
- əlavə armor;
- gameplay üstünlüyü

satılmamalıdır.

### Server Authority

Client heç vaxt kritik gameplay məlumatının son qərarvericisi olmamalıdır.

### Performance First

Vizual keyfiyyət multiplayer performansını pozmamalıdır.

### Clear Visibility

Düşmən oyunçular xəritənin dekorasiyası içində itməməlidir.

---

# 6. Əsas gameplay loop

Əsas gameplay dövrəsi:

```text
Oyuna giriş
↓
Main Menu
↓
Party yarat / Solo
↓
Game Mode seç
↓
Matchmaking
↓
Server seçilir
↓
Xəritə yüklənir
↓
Warm-up
↓
Round başlayır
↓
Buy Phase
↓
Combat
↓
Objective
↓
Round nəticəsi
↓
Economy update
↓
Növbəti round
↓
Match nəticəsi
↓
XP / Rank / Stats
↓
Main Menu
```

---

# 7. Komandalar

İki əsas faction olacaq.

İşçi adlar:

### Alpha

Hücum edən tərəf.

### Bravo

Müdafiə edən tərəf.

Daha sonra bu komandalar NaxcivanCS lore-na uyğun adlandırıla bilər.

Komanda adları real siyasi, hərbi və ya dövlət qurumlarının birbaşa surəti olmamalıdır.

---

# 8. Əsas oyun rejimi — Bomb / Defuse

Match:

```text
5 vs 5
```

Xəritədə iki objective sahəsi:

```text
Site A
Site B
```

Hücum edən komanda:

- bomba daşıyır;
- A və ya B site-a çatmalıdır;
- bombanı yerləşdirməlidir;
- partlayana qədər müdafiə etməlidir.

Müdafiə edən komanda:

- hücum edənləri dayandırmalıdır;
- bomba yerləşdirilərsə onu defuse etməlidir.

---

# 9. Match strukturu

İlkin competitive format:

```text
5v5
MR12
```

Yəni hər tərəf maksimum 12 əsas round oynayır.

Təklif olunan qələbə:

```text
13 round qazanan komanda qalibdir.
```

12:12 olduqda overtime tətbiq edilə bilər.

Məsələn:

```text
Overtime:
3 round attack
3 round defense
```

---

# 10. Round sistemi

Hər round aşağıdakı mərhələlərdən ibarətdir:

```text
Freeze Time
↓
Buy Time
↓
Active Round
↓
Bomb Planted
↓
Round End
```

Təklif olunan default:

| Parametr | Dəyər |
|---|---:|
| Freeze time | 10 saniyə |
| Round time | 1:45 |
| Bomb timer | 40 saniyə |
| Round end delay | 5 saniyə |
| Buy time | 20 saniyə |

Bütün dəyərlər server config vasitəsilə dəyişdirilə bilməlidir.

---

# 11. Oyunçu movement sistemi

Oyunçu aşağıdakı hərəkətləri edə bilməlidir:

- walk;
- run;
- crouch;
- jump;
- strafe;
- ladder movement;
- slope movement.

Default controls:

```text
W = forward
S = backward
A = left
D = right
Space = jump
Ctrl = crouch
Shift = walk
R = reload
E = interact
G = drop weapon
B = buy menu
Tab = scoreboard
```

Movement sistemi deterministic-ə mümkün qədər yaxın olmalıdır.

---

# 12. Movement skill

Competitive gameplay üçün aşağıdakılar nəzərə alınmalıdır:

- acceleration;
- deceleration;
- air control;
- landing slowdown;
- movement accuracy penalty;
- crouch accuracy;
- walk accuracy.

Silahla qaçaraq atəş açmaq əksər silahlarda qeyri-dəqiq olmalıdır.

---

# 13. Kamera sistemi

First-person kamera istifadə ediləcək.

Funksiyalar:

- configurable FOV;
- recoil camera;
- weapon sway;
- head bob;
- damage feedback;
- spectator camera.

Default:

```text
FOV: 90
```

Configurable:

```text
75–110
```

---

# 14. Silah sistemi

Silah kateqoriyaları:

### Pistols

- standard pistol;
- heavy pistol;
- burst pistol.

### SMG

- compact SMG;
- high-fire-rate SMG.

### Rifles

- assault rifle;
- precision rifle;
- burst rifle.

### Sniper

- light sniper;
- heavy sniper.

### Shotguns

- pump shotgun;
- semi-auto shotgun.

### Heavy

- machine gun.

### Equipment

- knife;
- grenade;
- smoke;
- flash;
- incendiary;
- bomb;
- defuse kit.

Silahlar real dünyadakı modellərin birbaşa trademark adlarını istifadə etmədən fictional adlarla təqdim edilə bilər.

---

# 15. Weapon component architecture

```text
WeaponBase
│
├── WeaponData
├── FireController
├── AmmoController
├── ReloadController
├── RecoilController
├── SpreadController
├── AnimationController
├── AudioController
└── NetworkWeaponController
```

---

# 16. WeaponData modeli

Hər silah üçün:

```text
id
name
category
damage
armorPenetration
fireRate
magazineSize
reserveAmmo
reloadTime
movementSpeed
spreadStanding
spreadMoving
spreadJumping
recoilPattern
range
damageFalloff
price
killReward
```

---

# 17. Recoil sistemi

Recoil random olmamalıdır.

Əsas hissə predefined pattern olmalıdır.

Məsələn:

```text
Shot 1 → vertical
Shot 2 → vertical
Shot 3 → right
Shot 4 → right
Shot 5 → left
...
```

Randomness yalnız müəyyən kiçik limit daxilində istifadə edilə bilər.

Bu, oyunçunun spray pattern öyrənməsinə imkan verəcək.

---

# 18. Accuracy sistemi

Accuracy aşağıdakılardan təsirlənəcək:

```text
movement velocity
stance
jump state
weapon recoil
burst duration
weapon type
```

Misal:

Standing:

```text
100% baseline
```

Moving:

```text
accuracy penalty
```

Jumping:

```text
major penalty
```

Crouching:

```text
accuracy bonus
```

---

# 19. Damage sistemi

Hit bölgələri:

```text
Head
Chest
Stomach
Arms
Legs
```

Damage multiplier:

| Hitbox | Multiplier |
|---|---:|
| Head | ~4x |
| Chest | 1x |
| Stomach | 1.2x |
| Arms | 1x |
| Legs | 0.75x |

Balans testləri nəticəsində dəyişdirilməlidir.

---

# 20. Armor sistemi

Oyunçuda:

```text
Health: 100
Armor: 0–100
```

Equipment:

```text
Armor Vest
Helmet + Armor
```

Armor bullet damage-i azaldır.

Helmet headshot nəticəsinə təsir göstərə bilər.

---

# 21. Penetration sistemi

Gələcək versiyada material penetration əlavə edilə bilər.

Materiallar:

```text
wood
thin metal
glass
drywall
concrete
stone
```

Hər material:

```text
penetrationResistance
damageReduction
maxThickness
```

---

# 22. Grenade sistemi

Grenade növləri:

### Fragmentation

Area damage.

### Flash

Görmə və eşitmə effektinə təsir edir.

### Smoke

Dynamic smoke volume.

### Incendiary

Müəyyən sahəni müvəqqəti bloklayır.

---

# 23. Smoke sistemi

Smoke competitive gameplay üçün çox vacibdir.

Smoke:

- server tərəfindən yaradılmalıdır;
- bütün client-lərdə eyni mövqedə olmalıdır;
- gameplay visibility-ni düzgün bloklamalıdır.

Visual-only smoke istifadə edilməməlidir.

---

# 24. Flash sistemi

Flash intensity aşağıdakılardan hesablanmalıdır:

```text
distance
view angle
line of sight
obstacles
```

---

# 25. Economy sistemi

Hər oyunçunun match daxilində pulu olacaq.

Məsələn:

```text
Start Money: 800
Maximum Money: 16000
```

Pul qazanma yolları:

- round win;
- round loss;
- kill;
- objective;
- bomb plant;
- defuse.

---

# 26. Loss bonus

Ardıcıl round uduzan komanda əlavə bonus almalıdır.

Bu, comeback mexanizmi yaradır.

Misal:

```text
Loss 1 → 1400
Loss 2 → 1900
Loss 3 → 2400
Loss 4 → 2900
Loss 5+ → 3400
```

Rəqəmlər balans mərhələsində dəyişə bilər.

---

# 27. Buy sistemi

Buy menu kateqoriyaları:

```text
Pistols
SMG
Rifles
Snipers
Shotguns
Heavy
Equipment
Grenades
```

Buy yalnız müəyyən zonada və vaxtda mümkün olmalıdır.

---

# 28. Inventory

Oyunçu:

```text
1 Primary
1 Secondary
1 Knife
Grenades
Bomb / objective item
```

daşıya bilər.

---

# 29. Weapon pickup

Yerdəki silahlar götürülə bilməlidir.

Server yoxlamalıdır:

```text
distance
weapon existence
inventory availability
ownership state
```

---

# 30. Map design

NaxcivanCS-in əsas fərqləndirici elementlərindən biri Naxçıvan atmosferidir.

Xəritələr real yerlərin 1:1 surəti olmamalıdır.

Real memarlıqdan və coğrafiyadan ilhamlanan fictional competitive məkanlar yaradılmalıdır.

---

# 31. İlkin xəritələr

## NC_Qala

Naxçıvan memarlığından ilhamlanan şəhər xəritəsi.

Tema:

- daş divarlar;
- dar küçələr;
- həyətlər;
- açıq meydanlar.

Gameplay:

```text
Medium range
Close combat
Balanced rotations
```

---

## NC_Duzdag

Duzdağ atmosferindən ilhamlanan industrial/underground xəritə.

Tema:

- tunellər;
- duz mağaraları;
- industrial equipment;
- underground routes.

Gameplay:

```text
Close-medium range
Vertical angles
Tight chokepoints
```

---

## NC_Araz

Araz vadisi və logistika məntəqəsindən ilhamlanan xəritə.

Tema:

- warehouse;
- railway;
- cargo;
- industrial yard.

Gameplay:

```text
Long sightlines
Open spaces
Sniper-friendly
```

---

## NC_Ordubad

Ordubad memarlığı və bağlarından ilhamlanan tactical map.

Tema:

- həyətlər;
- daş küçələr;
- bağlar;
- kiçik tikililər.

---

## NC_Batabat

Dağ və təbiət temalı xəritə.

Gameplay daha çox Casual / Deathmatch üçün uyğunlaşdırıla bilər.

---

# 32. Competitive map qaydaları

Hər xəritə:

- minimum iki bombsite;
- spawn protection;
- balanslı rotations;
- predictable timings;
- clear callouts;
- visibility standards;
- optimization zones

tələblərinə uyğun olmalıdır.

---

# 33. Map callout sistemi

Hər xəritənin rəsmi callout xəritəsi olmalıdır.

Misal:

```text
A Site
B Site
Mid
Connector
Tunnel
Ramp
Long
Short
Heaven
Lower
Upper
Spawn
```

---

# 34. Casual Mode

Format:

```text
10v10
```

Funksiyalar:

- daha rahat economy;
- daha qısa matchmaking;
- rank itkisi yoxdur;
- yeni oyunçular üçün əlverişli.

---

# 35. Deathmatch

Gameplay:

```text
Free respawn
Fast weapon selection
Score based
10–15 minutes
```

İstifadə məqsədi:

- aim practice;
- warmup;
- weapon practice.

---

# 36. Training Mode

Offline training map.

Funksiyalar:

- bots;
- target practice;
- recoil practice;
- grenade practice;
- movement practice.

---

# 37. Bot sistemi

Bots MVP-də sadə davranışa malik olacaq.

Bot state machine:

```text
Idle
Patrol
Search
Engage
Retreat
Plant
Defuse
```

Gələcəkdə behavior tree istifadə edilə bilər.

---

# 38. Difficulty

Bot səviyyələri:

```text
Easy
Normal
Hard
Expert
```

---

# 39. Multiplayer arxitekturası

NaxcivanCS üçün **Server Authoritative Dedicated Server** modeli istifadə edilməlidir.

```text
Client
   │
   │ UDP / ENet
   ▼
Dedicated Game Server
   │
   ├── Movement validation
   ├── Weapon validation
   ├── Hit validation
   ├── Economy
   ├── Round logic
   └── Match state
```

Client yalnız input göndərir.

Server nəticəni hesablayır.

---

# 40. Niyə Peer-to-Peer istifadə edilməməlidir?

Competitive FPS üçün P2P:

- host advantage yaradır;
- cheating asanlaşır;
- host disconnect problemi yaradır;
- network authority zəifləyir.

Buna görə Dedicated Server məcburidir.

---

# 41. Network transport

Tövsiyə:

```text
Godot ENet
UDP
```

Realtime gameplay üçün WebSocket əsas transport istifadə edilməməlidir.

WebSocket:

- account;
- notification;
- lobby;

kimi sistemlər üçün istifadə edilə bilər.

---

# 42. Tick rate

İlkin server tickrate:

```text
64 tick
```

Server hər:

```text
~15.6 ms
```

game simulation update edir.

Gələcəkdə:

```text
128 tick
```

competitive premium serverlər araşdırıla bilər.

---

# 43. Client prediction

Movement hissinin responsive olması üçün:

```text
Client Prediction
+
Server Reconciliation
```

istifadə edilməlidir.

---

# 44. Interpolation

Digər oyunçular üçün:

```text
snapshot interpolation
```

tətbiq edilməlidir.

Bu network jitter-i gizlədir.

---

# 45. Lag compensation

Server keçmiş oyunçu mövqelərini qısa müddət saxlamalıdır.

Məsələn:

```text
200 ms history buffer
```

Fire event gəldikdə server oyunçunun latency-sini nəzərə alaraq hit detection edə bilər.

---

# 46. Hit registration

Client:

```text
“I hit player X”
```

deməməlidir.

Client yalnız:

```text
fire input
aim direction
timestamp
```

göndərməlidir.

Server hit-i özü hesablamalıdır.

---

# 47. Anti-cheat

Anti-cheat yalnız client proqramı olmamalıdır.

Əsas müdafiə server-side olacaq.

Server yoxlamaları:

- impossible movement;
- speed hack;
- teleport;
- impossible fire rate;
- impossible ammo;
- invalid weapon;
- impossible damage;
- invalid buy;
- invalid economy;
- suspicious aim;
- impossible angle changes.

---

# 48. Cheat detection scoring

Hər istifadəçinin internal:

```text
SuspicionScore
```

dəyəri ola bilər.

Misal:

```text
Impossible movement +20
Invalid fire rate +30
Repeated impossible hits +15
Tampered client +40
```

Threshold:

```text
80 → review
100 → temporary restriction
```

Avtomatik permanent ban ilk mərhələdə istifadə edilməməlidir.

---

# 49. Report sistemi

Oyunçu report səbəbləri:

```text
Cheating
Toxicity
Griefing
AFK
Spam
Abusive Voice
Abusive Text
```

---

# 50. Trust sistemi

Gələcəkdə oyunçu davranış skoru:

```text
TrustScore
```

yaradıla bilər.

Matchmaking istifadə edə bilər.

---

# 51. Authentication

Account sistemi:

```text
Email
Username
Password
```

Gələcək:

```text
Steam Login
Google
Discord
```

---

# 52. Username qaydaları

Username:

```text
3–20 characters
```

Qadağan:

- offensive names;
- administrator impersonation;
- trademark abuse;
- control characters.

---

# 53. Player profile

Profil:

```text
Avatar
Username
Level
Rank
XP
Matches
Wins
Losses
Kills
Deaths
K/D
Headshot %
Accuracy
MVP
Play Time
```

---

# 54. XP sistemi

XP aşağıdakılara görə verilə bilər:

```text
match completion
round win
kills
assists
objective
MVP
```

XP competitive rank-dən ayrıdır.

---

# 55. Player level

Misal:

```text
Level 1–100
```

Level gameplay üstünlüyü verməməlidir.

---

# 56. Competitive Rank

İlkin rank sistemi:

```text
Recruit
Bronze I
Bronze II
Bronze III
Silver I
Silver II
Silver III
Gold I
Gold II
Gold III
Platinum
Diamond
Elite
Master
Legend
```

---

# 57. Rating sistemi

Arxa planda numeric rating saxlanmalıdır.

Məsələn:

```text
MMR
```

Rank yalnız MMR-in vizual representation-u olacaq.

---

# 58. Matchmaking

Matchmaking nəzərə almalıdır:

```text
MMR
Ping
Party size
Region
Trust score
Queue time
```

---

# 59. Matchmaking flow

```text
Player queues
↓
MMR range selected
↓
Region checked
↓
Ping checked
↓
10 compatible players
↓
Game server allocated
↓
Match created
↓
Players receive server token
↓
Clients connect
```

---

# 60. Region sistemi

İlk mərhələdə mümkün regionlar:

```text
AZ / Caucasus
Turkey
Central Europe
```

Server regionu oyunçunun ping nəticəsinə görə seçilməlidir.

---

# 61. Party sistemi

Party:

```text
1–5 players
```

Funksiyalar:

- invite;
- kick;
- leave;
- ready state;
- party leader;
- queue together.

---

# 62. Friend sistemi

Funksiyalar:

```text
Friend Request
Accept
Decline
Remove
Block
Invite to Party
View Status
```

Status:

```text
Offline
Online
In Lobby
Searching
In Match
```

---

# 63. Lobby

Lobby ekranında:

```text
Party members
Selected mode
Selected region
Queue status
Average ping
```

görünməlidir.

---

# 64. Voice chat

İlkin versiyada optional.

Kanallar:

```text
Team Voice
Party Voice
```

Enemy team match zamanı voice eşitməməlidir.

---

# 65. Text chat

Kanallar:

```text
Team
All
Party
```

Mute sistemi olmalıdır.

---

# 66. Moderation

Admin aşağıdakı imkanlara malik olmalıdır:

```text
Warn
Mute
Kick
Temporary Ban
Permanent Ban
Unban
Review Reports
View Match
```

---

# 67. Spectator sistemi

Ölən oyunçu yalnız komanda yoldaşlarını izləyə bilər.

Competitive rejimdə enemy spectating olmamalıdır.

Camera modes:

```text
First Person
Third Person
Free Camera — admin/replay only
```

---

# 68. Replay sistemi

Server match event-lərini saxlaya bilər.

Tam video yazmaq əvəzinə:

```text
snapshots
inputs
events
```

saxlanmalıdır.

Replay engine sonradan match-i reconstruct edə bilər.

---

# 69. Match history

Hər match üçün:

```text
map
mode
date
duration
score
players
kills
deaths
assists
MVP
headshots
```

saxlanmalıdır.

---

# 70. Leaderboard

Leaderboard kateqoriyaları:

```text
Global
Country
Friends
Season
```

Metrics:

```text
Rank
MMR
Wins
K/D
Headshots
```

---

# 71. Season sistemi

Competitive rank season əsaslı ola bilər.

Misal:

```text
Season duration: 3 months
```

Season sonunda:

- rank soft reset;
- seasonal badge;
- leaderboard archive.

---

# 72. Achievement sistemi

Achievement nümunələri:

```text
First Blood
100 Kills
1000 Kills
Ace
Clutch 1v3
Clutch 1v4
Clutch 1v5
Bomb Expert
Defuse Master
Headshot Master
```

---

# 73. Cosmetics

Monetizasiya əsasən cosmetics üzərindən qurula bilər.

Cosmetic növləri:

```text
Weapon skins
Gloves
Player cards
Badges
Sprays
Profile frames
Music packs
```

Heç biri gameplay advantage verməməlidir.

---

# 74. Marketplace

MVP-də marketplace qurulmamalıdır.

İlk mərhələdə:

```text
Direct Store
```

daha təhlükəsiz və sadədir.

Marketplace gələcəkdə ayrıca hüquqi və iqtisadi analiz tələb edir.

---

# 75. Loot box

İlkin versiyada loot box tövsiyə edilmir.

Səbəblər:

- hüquqi risk;
- gambling qaydaları;
- yaş məhdudiyyətləri;
- store platform policy-ləri.

---

# 76. UI/UX

Əsas menu:

```text
PLAY
INVENTORY
PROFILE
RANK
FRIENDS
SETTINGS
EXIT
```

---

# 77. Main Menu

Main menu minimalist olmalıdır.

Ekranda:

- current player;
- rank;
- party;
- announcements;
- play button;
- friend list.

---

# 78. HUD

HUD:

```text
Health
Armor
Ammo
Weapon
Grenades
Money
Round timer
Team score
Kill feed
Minimap
```

Competitive HUD mümkün qədər təmiz olmalıdır.

---

# 79. Crosshair

Tam customizable:

```text
size
thickness
gap
outline
opacity
dynamic/static
```

---

# 80. Settings

### Video

```text
Resolution
Display Mode
VSync
FPS Limit
Texture Quality
Shadow Quality
Effects
Anti-aliasing
FOV
```

### Audio

```text
Master
Music
Effects
Voice
Footsteps
```

### Controls

```text
Sensitivity
Raw Input
Key Bindings
Invert Mouse
```

---

# 81. Performance

Hədəf:

### Minimum PC

```text
1080p Low
60 FPS
```

### Recommended PC

```text
1080p Medium/High
144 FPS
```

Competitive mode üçün:

```text
200+ FPS
```

mümkün olduğu qədər optimallaşdırılmalıdır.

---

# 82. Rendering

Competitive FPS üçün:

- baked lighting;
- optimized shadows;
- occlusion culling;
- LOD;
- mesh batching;
- texture atlases

istifadə edilməlidir.

---

# 83. Audio

Audio gameplay-in kritik hissəsidir.

Spatial audio aşağıdakıları düzgün verməlidir:

```text
footsteps
gunfire
reload
grenade
bomb
doors
surface sounds
```

---

# 84. Surface audio

Materiallar:

```text
Stone
Wood
Metal
Glass
Grass
Sand
Water
```

hər biri fərqli footstep səsinə malik olmalıdır.

---

# 85. Oyun mühərriki

Tövsiyə:

```text
Godot 4
C#
```

Səbəblər:

- open source;
- lisenziya baxımından əlverişli;
- C# dəstəyi;
- dedicated server export;
- ENet multiplayer;
- Windows/Linux export;
- aşağı engine xərci.

---

# 86. Client architecture

```text
NaxcivanCS.Client
├── Core
├── Player
├── Weapons
├── Gameplay
├── Network
├── UI
├── Audio
├── Maps
├── Effects
└── Services
```

---

# 87. Server architecture

Dedicated server:

```text
NaxcivanCS.Server
├── ServerCore
├── Network
├── Match
├── Players
├── Weapons
├── Damage
├── Economy
├── Objectives
├── AntiCheat
└── Replay
```

Server Linux headless rejimində işləməlidir.

---

# 88. Backend architecture

Tövsiyə olunan backend:

```text
ASP.NET Core
PostgreSQL
Redis
```

Architecture:

```text
Client
 │
 ├──── HTTPS ────> API
 │                  │
 │                  ├── PostgreSQL
 │                  └── Redis
 │
 └──── UDP ─────> Game Server
                    │
                    └── Backend API
```

---

# 89. Cloudflare istifadəsi

Cloudflare aşağıdakılar üçün istifadə edilə bilər:

```text
DNS
CDN
WAF
Rate Limiting
R2
Turnstile
Website
Launcher API protection
```

Cloudflare Workers:

- public API gateway;
- web portal;
- lightweight services

üçün istifadə edilə bilər.

Realtime FPS simulation Workers üzərində yerləşdirilməməlidir.

---

# 90. PostgreSQL

Əsas persistent database:

```text
PostgreSQL
```

Saxlanacaq:

- accounts;
- player profiles;
- stats;
- matches;
- ranks;
- reports;
- bans;
- inventory;
- purchases.

---

# 91. Redis

Redis:

```text
sessions
matchmaking queues
online status
party state
rate limits
server state
```

üçün istifadə edilə bilər.

---

# 92. Database modeli

Əsas cədvəllər:

```text
users
player_profiles
player_stats
player_settings
friends
friend_requests
parties
party_members
matches
match_players
match_rounds
weapons
player_inventory
cosmetics
ranks
rank_history
reports
bans
servers
seasons
leaderboards
transactions
```

---

# 93. users

```text
id UUID
username VARCHAR
email VARCHAR
password_hash VARCHAR
status ENUM
created_at
updated_at
last_login_at
```

---

# 94. player_stats

```text
user_id
matches
wins
losses
kills
deaths
assists
headshots
shots_fired
shots_hit
bomb_plants
bomb_defuses
mvps
play_time
```

---

# 95. matches

```text
id
server_id
mode
map
region
started_at
ended_at
team_a_score
team_b_score
winner
status
```

---

# 96. match_players

```text
match_id
user_id
team
kills
deaths
assists
headshots
damage
mvps
score
mmr_before
mmr_after
```

---

# 97. bans

```text
id
user_id
reason
type
starts_at
ends_at
issued_by
evidence
```

---

# 98. Game server registry

Hər game server backend-ə heartbeat göndərməlidir.

Server data:

```text
serverId
region
ip
port
capacity
players
status
version
lastHeartbeat
```

---

# 99. Server orchestration

MVP:

```text
Docker
Linux VPS
```

Gələcək:

```text
Docker
Kubernetes
Agones
```

İlk versiyada Kubernetes tələb olunmur.

---

# 100. Docker

Dedicated server image:

```dockerfile
NaxcivanCS-server
```

Hər match ayrıca container-da işlədilə bilər.

---

# 101. Server lifecycle

```text
Backend requests server
↓
Container starts
↓
Server registers
↓
Players connect
↓
Match starts
↓
Match ends
↓
Stats uploaded
↓
Replay uploaded
↓
Container destroyed
```

---

# 102. Game versioning

Client və server:

```text
protocolVersion
gameVersion
contentVersion
```

yoxlamalıdır.

Uyğun olmayan client serverə qoşulmamalıdır.

---

# 103. Launcher

Gələcəkdə NaxcivanCS Launcher yaradıla bilər.

Funksiyalar:

```text
Login
Install
Update
Repair
Verify Files
Launch Game
News
Server Status
```

---

# 104. Patch sistemi

Bütün oyunu yenidən yükləmək əvəzinə delta patch sistemi hazırlanmalıdır.

Asset paketləri ayrıca versionlaşdırılmalıdır.

---

# 105. R2 istifadəsi

Cloudflare R2:

```text
game patches
launcher assets
screenshots
replays
logs archive
cosmetic images
```

üçün istifadə oluna bilər.

---

# 106. Website

NaxcivanCS üçün ayrıca:

```text
naxcivancs.az
```

kimi domain istifadə edilə bilər — mövcudluq ayrıca yoxlanmalıdır.

Sayt:

```text
Home
Download
News
Leaderboard
Stats
Support
Rules
Privacy
Terms
Server Status
```

---

# 107. Admin Panel

Admin panel ciddi şəkildə role-based olmalıdır.

Rollər:

### Super Admin

Tam idarəetmə.

### Administrator

Player və server idarəetməsi.

### Moderator

Report və ban sistemi.

### Support

Account support.

---

# 108. Admin Dashboard

Dashboard:

```text
Online Players
Active Matches
Active Servers
Queue Size
Reports
Bans Today
New Accounts
Server CPU
Server RAM
Average Ping
```

---

# 109. Player moderation panel

Admin görə bilməlidir:

```text
Account
Match history
Reports
Ban history
IP history
Device identifiers
Suspicion score
Recent servers
```

Device fingerprint privacy qaydalarına uyğun qurulmalıdır.

---

# 110. Logging

Log növləri:

```text
Auth
Match
Server
Economy
Admin
AntiCheat
Error
Security
```

Log-lar immutable formatda arxivlənməlidir.

---

# 111. Security

Backend:

- HTTPS only;
- JWT/session validation;
- refresh-token rotation;
- password hashing;
- rate limiting;
- CSRF protection where relevant;
- input validation;
- SQL injection protection;
- audit logs.

Password hashing:

```text
Argon2id
```

tövsiyə olunur.

---

# 112. DDoS

Game server DDoS ciddi riskdir.

Infrastructure provider:

- UDP DDoS protection;
- network filtering;
- rate limiting

dəstəkləməlidir.

Game server IP-ləri mümkün qədər backend vasitəsilə idarə edilməlidir.

---

# 113. Telemetry

Anonymous gameplay telemetry:

```text
FPS
Ping
Packet loss
Crash
Map loading time
Server tick time
Match abandonment
```

toplana bilər.

Privacy policy-də açıq şəkildə göstərilməlidir.

---

# 114. Crash reporting

Client crash zamanı:

```text
version
OS
GPU
driver
stack trace
scene
```

toplana bilər.

Şəxsi məlumat toplanmamalıdır.

---

# 115. Analytics

Əsas məhsul KPI-ları:

```text
DAU
WAU
MAU
D1 retention
D7 retention
D30 retention
Average session
Matches/player
Queue time
Match completion
Crash rate
Cheat report rate
```

---

# 116. Əsas texniki KPI

### Game Client

```text
Crash-free sessions > 99.5%
```

### Game Server

```text
Tick stability > 99%
```

### Matchmaking

```text
Median queue < 90 sec
```

### Network

```text
Packet loss < 2%
```

---

# 117. Accessibility

Aşağıdakılar nəzərdə tutulmalıdır:

- colorblind mode;
- scalable HUD;
- subtitles;
- separate voice volume;
- motion reduction;
- configurable crosshair.

---

# 118. Localization

Architecture:

```text
/localization
    az.json
    en.json
    ru.json
```

String-lər kodda hardcode edilməməlidir.

---

# 119. Azərbaycan dili

NaxcivanCS-in default dili:

```text
Azərbaycan dili
```

ola bilər.

İstifadəçi dəyişə bilər.

---

# 120. Minimum sistem tələbləri — ilkin hədəf

### Minimum

```text
Windows 10/11 64-bit
4-core CPU
8 GB RAM
GTX 1050 / RX 560 səviyyəsi
5–10 GB storage
Broadband internet
```

### Recommended

```text
Windows 11
6-core CPU
16 GB RAM
GTX 1660 / RX 6600 və ya daha yaxşı
SSD
```

Real tələblər profiling nəticəsindən sonra müəyyən edilməlidir.

---

# 121. Repository strukturu

```text
NaxcivanCS/
│
├── client/
│
├── server/
│
├── backend/
│
├── launcher/
│
├── shared/
│
├── infrastructure/
│
├── tools/
│
├── docs/
│
├── tests/
│
└── README.md
```

---

# 122. Git workflow

Branch-lər:

```text
main
develop
feature/*
fix/*
release/*
```

Workflow:

```text
Issue
↓
Branch
↓
Commit
↓
Pull Request
↓
CI
↓
Review
↓
Merge
```

Solo development zamanı mandatory approval tələb edilməyə bilər, lakin CI keçməlidir.

---

# 123. CI/CD

GitHub Actions:

### Client

```text
build
tests
lint
export
```

### Server

```text
build
tests
docker
security scan
```

### Backend

```text
restore
build
test
publish
docker
```

---

# 124. Automated tests

Unit test:

```text
Economy
Damage
Weapon stats
Rank calculation
Match state
Authentication
```

Integration test:

```text
Client → Game Server
Game Server → Backend
Backend → DB
```

---

# 125. Network tests

Test ssenariləri:

```text
20 ms latency
50 ms
100 ms
150 ms
200 ms

0% loss
1% loss
3% loss
5% loss
```

Gameplay davranışı ölçülməlidir.

---

# 126. Development mərhələləri

## Phase 0 — Pre-production

Hazırlanacaq:

- game design;
- movement prototype;
- weapon prototype;
- networking prototype;
- art direction;
- architecture.

---

# 127. Phase 1 — Prototype

Funksiyalar:

```text
1 test map
2 players
movement
shooting
damage
death
respawn
basic network
```

Məqsəd:

> “Gunplay fun-dırmı?”

---

# 128. Phase 2 — Multiplayer Core

Əlavə ediləcək:

```text
Dedicated server
5v5
Round system
Teams
Weapon sync
Damage
Scoreboard
```

---

# 129. Phase 3 — Bomb Mode

Əlavə:

```text
Bomb
Plant
Defuse
Sites
Round victory
Economy
Buy menu
```

Bu mərhələdə oyun artıq ilkin NaxcivanCS formasını almalıdır.

---

# 130. Phase 4 — Backend

Əlavə:

```text
Accounts
Profiles
Stats
Match history
Friends
Party
```

---

# 131. Phase 5 — Matchmaking

Əlavə:

```text
Queue
MMR
Server allocation
Regions
Competitive ranking
```

---

# 132. Phase 6 — Alpha

Alpha versiyası:

```text
3 maps
10–15 weapons
Competitive
Casual
Deathmatch
Stats
Rank
Admin panel
Basic anti-cheat
```

---

# 133. Phase 7 — Closed Beta

İstifadəçi sayı:

```text
100–1000 players
```

Test:

- server load;
- matchmaking;
- cheating;
- balance;
- performance;
- crash telemetry.

---

# 134. Phase 8 — Open Beta

Əlavə:

```text
Launcher
Store
Cosmetics
Season
Leaderboard
Replay
Expanded moderation
```

---

# 135. Version 1.0

Launch kriteriyaları:

- stabil 5v5 competitive;
- minimum 5 competitive map;
- minimum 20 silah;
- matchmaking;
- ranking;
- anti-cheat;
- party;
- friends;
- stats;
- replay;
- moderation;
- scalable dedicated servers.

---

# 136. MVP scope

İlk oynanıla bilən MVP üçün yalnız:

```text
Windows
1 map
5v5
1 game mode
6–8 weapons
Bomb
Buy menu
Economy
Dedicated server
Scoreboard
Basic settings
```

lazımdır.

MVP-yə aşağıdakılar daxil edilməməlidir:

```text
skins
marketplace
battle pass
clans
tournaments
advanced ranking
voice
replay
```

Əvvəlcə gameplay işləməlidir.

---

# 137. MVP Acceptance Criteria

MVP uğurlu hesab olunur əgər:

1. 10 oyunçu serverə qoşula bilir.
2. 5v5 stabil oynanılır.
3. Server authoritative movement işləyir.
4. Weapon firing düzgün sync olunur.
5. Hit registration sabitdir.
6. Bomb plant/defuse işləyir.
7. Round reset problemsizdir.
8. Economy düzgün hesablanır.
9. Server 60+ tick-ə yaxın stabil işləyir.
10. 60 dəqiqəlik testdə critical crash yoxdur.

---

# 138. Alpha Acceptance Criteria

Alpha:

```text
>99% match completion
<2% server crash
<3% client crash
stable matchmaking
basic anti-cheat
3 playable maps
```

---

# 139. Risklər

Əsas texniki risklər:

### Networking

FPS networking kompleksdir.

### Hit Registration

Pis hit-reg oyunçuların etibarını çox tez itirə bilər.

### Cheating

Free-to-play model cheat problemlərini artıra bilər.

### Player population

5v5 matchmaking üçün minimum aktiv istifadəçi bazası tələb olunur.

### Server cost

Dedicated server xərcləri istifadəçi sayına görə artır.

---

# 140. Risk mitigation

### Networking

Əvvəlcə networking prototype qurulmalıdır.

### Anti-cheat

Gameplay logic server-authoritative olmalıdır.

### Server cost

Auto-scaling server pool istifadə edilməlidir.

### Player population

Launch zamanı Casual və Competitive queue-lar həddindən artıq parçalanmamalıdır.

---

# 141. Branding

NaxcivanCS vizual dili:

```text
Dark
Industrial
Stone
Copper
Red accents
Modern tactical
```

Naxçıvan memarlığından:

- daş;
- kərpic;
- geometrik ornament;
- dağ tonları

ilham alına bilər.

---

# 142. Logo

Logo:

```text
NAXCIVAN
CS
```

elementlərindən qurula bilər.

Lakin Counter-Strike loqosu və tipografiyası kopyalanmamalıdır.

---

# 143. Audio branding

Original:

- menu music;
- round start;
- round win;
- bomb warning;
- UI sounds

hazırlanmalıdır.

---

# 144. Legal/IP qaydası

NaxcivanCS müstəqil IP olmalıdır.

Qadağan:

- Counter-Strike xəritələrinin reproduksiyası;
- Valve asset-ləri;
- CS audio;
- CS logo;
- Valve character models;
- Valve weapon models;
- decompiled game code.

İlham almaq olar, birbaşa surət çıxarmaq olmaz.

---

# 145. Game design fərqləndiriciləri

NaxcivanCS yalnız “CS klonu” olmamalıdır.

Fərqləndirici xüsusiyyətlər:

### Regional Maps

Naxçıvan atmosferi.

### Advanced Training

Integrated recoil/grenade trainer.

### Community Servers

Gələcəkdə öz serverini açma imkanı.

### Tournament System

Native tournament support.

### Detailed Stats

Advanced match analytics.

---

# 146. Tournament sistemi — gələcək

Funksiyalar:

```text
Tournament create
Teams
Bracket
Match servers
Admin observers
Match history
Replay
Results
```

---

# 147. Clan sistemi

Sonrakı mərhələ:

```text
Clan
Clan tag
Members
Roles
Clan stats
Clan matches
```

---

# 148. Community Servers

Gələcəkdə istifadəçilər:

```text
NaxcivanCS Dedicated Server
```

download edə bilər.

Server config:

```text
map
mode
max_players
password
tickrate
friendly_fire
round_time
```

---

# 149. Modding

Version 1.0-dan sonra mod SDK araşdırıla bilər.

Modding oyun community-sinin böyüməsinə kömək edə bilər.

---

# 150. Server browser

Community server sistemi əlavə ediləndə:

```text
Name
Map
Mode
Players
Ping
Region
Password
```

göstərilməlidir.

---

# 151. Development prioriteti

Prioritet sırası:

```text
Networking
↓
Movement
↓
Gunplay
↓
Hit Registration
↓
Round Logic
↓
Bomb
↓
Economy
↓
Maps
↓
Backend
↓
Matchmaking
↓
Ranking
↓
Cosmetics
```

Ən böyük səhv əvvəlcə:

- store;
- skin;
- animation;
- website

üzərində işləyib core gunplay-i gecikdirmək olar.

NaxcivanCS-in uğuru ilk növbədə:

```text
movement + shooting + networking
```

üçlüyündən asılı olacaq.

---

# 152. İlk texniki milestone

İlk repository milestone:

## NaxcivanCS Prototype 0.1

Tələblər:

```text
Godot 4 C#
Windows client
Linux dedicated server
ENet multiplayer
2 players
movement
mouse look
1 rifle
shooting
health
damage
death
respawn
ping display
```

Bu build yaradılmadan backend və ranking üzərində ciddi işə başlanmamalıdır.

---

# 153. Prototype folder structure

```text
/client
    /Scripts
        /Core
        /Player
        /Weapons
        /Network
        /UI

/server
    /Scripts
        /Core
        /Network
        /Gameplay

/shared
    /Models
    /Enums
    /Constants
```

---

# 154. Coding standartları

C#:

```text
PascalCase → classes/methods
camelCase → local variables
_privateField → private fields
IInterface → interfaces
```

Nullable reference types aktiv olmalıdır.

Warnings CI-də nəzarətdə saxlanmalıdır.

---

# 155. Configuration

Game balance rəqəmləri kod daxilində hardcode edilməməlidir.

Misal:

```text
weapon_rifle_01.json
```

```json
{
  "damage": 34,
  "fireRate": 600,
  "magazineSize": 30,
  "reloadTime": 2.4,
  "price": 2700
}
```

Beləliklə silah balansı game build dəyişmədən idarə edilə bilər.

---

# 156. Security rule

Client-a heç vaxt aşağıdakı səlahiyyət verilməməlidir:

```text
setHealth()
giveMoney()
giveWeapon()
setRank()
registerKill()
awardXP()
```

Bunların hamısını server/backend idarə etməlidir.

---

# 157. Final məhsul arxitekturası

```text
                         ┌──────────────────┐
                         │     Website      │
                         └────────┬─────────┘
                                  │
                           Cloudflare CDN
                                  │
                         ┌────────▼─────────┐
                         │   Backend API    │
                         │   ASP.NET Core   │
                         └───────┬──────────┘
                                 │
                     ┌───────────┴───────────┐
                     │                       │
              ┌──────▼───────┐       ┌──────▼─────┐
              │ PostgreSQL   │       │   Redis    │
              └──────────────┘       └────────────┘


        Windows Client
              │
              │ UDP / ENet
              ▼
       Dedicated Game Server
              │
              ├──── Match State
              ├──── Hit Validation
              ├──── Anti-Cheat
              ├──── Economy
              ├──── Round Logic
              │
              └──── HTTPS
                     │
                     ▼
                Backend API
```

---

# 158. Tövsiyə edilən yekun stack

## Game

```text
Godot 4
C#
ENet / UDP
```

## Game Server

```text
Godot Headless
C#
Linux
Docker
```

## Backend

```text
ASP.NET Core
C#
REST API
WebSocket — lobby/status
```

## Database

```text
PostgreSQL
```

## Cache / Realtime Backend

```text
Redis
```

## Infrastructure

```text
Docker
Linux VPS
Cloudflare
```

## Storage

```text
Cloudflare R2
```

## CI/CD

```text
GitHub Actions
Docker Registry
```

## Monitoring

```text
Prometheus
Grafana
Sentry/OpenTelemetry
```

---

# 159. Məhsulun əsas uğur şərti

NaxcivanCS-in ilk versiyasında yüzlərlə funksiya olmasına ehtiyac yoxdur.

Ən vacib dörd komponent:

```text
1. Movement
2. Gunplay
3. Hit Registration
4. Multiplayer Networking
```

bunlar mükəmməl işləməlidir.

Əgər bunlar yaxşıdırsa:

- xəritələr;
- ranking;
- cosmetics;
- tournament;
- clans;
- marketplace

sonradan əlavə edilə bilər.

Əgər əsas shooting və networking zəifdirsə, digər sistemlərin keyfiyyəti məhsulu xilas etməyəcək.

---

# 160. NaxcivanCS məhsul vizyonunun yekunu

NaxcivanCS-in məqsədi Counter-Strike-ın surətini yaratmaq deyil.

Məqsəd:

> Azərbaycan mənşəli, Naxçıvan atmosferinə sahib, sürətli, rəqabətli, aşağı latency-li və beynəlxalq səviyyədə oynanıla bilən müstəqil tactical FPS ekosistemi yaratmaqdır.

Məhsul uzunmüddətli olaraq aşağıdakı ekosistemə çevrilə bilər:

```text
NaxcivanCS Game
       │
       ├── Competitive
       ├── Casual
       ├── Deathmatch
       ├── Community Servers
       ├── Tournament Platform
       ├── Esports
       ├── Cosmetics
       ├── Player Profiles
       ├── Statistics
       ├── Replay
       └── Modding
```

Əsas texniki prinsip isə dəyişməməlidir:

> **Client input göndərir. Server qərar verir. Gameplay üstünlüyü satılmır. Competitive integrity qorunur.**