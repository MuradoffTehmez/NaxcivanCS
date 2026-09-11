# Problemlərin həlli

| Əlamət | Ehtimal / yoxlama | Addım |
|---|---|---|
| godot tapılmır | PATH və ya auto-discovery | GODOT_BIN-də .NET executable yolunu göstərin |
| C# script yüklənmir | Adi Godot və ya build yoxdur | .NET Godot və hər iki csproj build |
| Port dinlənilmir | 27015 artıq istifadə olunur | Köhnə öz server sessiyasını bağlayın və ya yeni port seçin |
| Qoşulma rədd edilir | Protocol/username | Hər iki tərəfdə 0.2.2/protocol 2; ad 3–20 simvol |
| Snapshot sıfırdır | Yanlış IP/UDP port və ya server yoxdur | Lokal server logu, eyni port və LAN ünvanı |
| Dünya/HUD yoxdur | Köhnə assembly/import və scene | Yenidən build/import; CanvasLayer strukturu |
| Mouse işləmir | Kursor sərbəstdir | Pəncərəyə klik; Esc buraxır |
| Atəş yoxdur | Reload, ammo, fokus və ya server | Reload bitsin, status/log yoxlanılsın |
| Səs gəlmir | Audio pipeline hazır deyil | 0.2.2 məhdudiyyətidir |
| Ammo yanlış görünür | Event-only unreliable sync | Known issue; server ammo qərarı ayrıdır |
| Divar arxasından damage | Occlusion hazır deyil | KI-01, gözlənən prototip məhdudiyyəti |
| --map dəyişmir | Arqument yalnız loglanır | Hazırda yalnız blockout var |
| JSON server ayarı işləmir | Loader yoxdur | Config səhifəsinin aktiv mənbə cədvəli |
| API 8080 cavab vermir | Launch profile fərqli port | --no-launch-profile --urls ilə başlat |
| Registry entry itir | 30 s heartbeat filtri/restart | Heartbeat yenilə; registry in-memory-dir |
| Wiki clone “not found” | İlk səhifə yoxdur və ya giriş səhvdir | GitHub-da ilkin Home saxla, repo girişini yoxla |

## Sıralı diaqnostika

Əvvəl versiyanı yazın: git rev-parse HEAD, dotnet --info, Godot --version. Sonra serveri tək başladın və error logunu yoxlayın. Ardınca bir client qoşun. Bu işləyəndən sonra ikinci client/LAN əlavə edin.

E2E script Debug assembly istifadə edir. Mənbə versiyası dəyişib log köhnə versiyanı göstərirsə, hər iki Godot layihəsini yenidən build edin. Import əməliyyatı C# build əvəzi deyil.

## Faydalı log hissələri

Serverin version banner-i, silah/blok sayı, qoşulma, peer ayrılması, ERROR və smoke result sətirlərini paylaşın. Bütöv istifadəçi qovluğu və credential fayllarını göndərməyin.

## Problem hesabatı

Gözlənilən nəticə, faktiki nəticə və minimal təkrar addımı yazın. “İşləmir” əvəzinə “0.2.2 client localhost:27015-ə qoşulur, handshake accepted olur, 6 saniyədə snapshot=0 qalır” kimi konkret müşahidə daha faydalıdır.

[Support](../../SUPPORT.md) · [Installation](Installation.md) · [Known issues](../KNOWN_ISSUES.md)
