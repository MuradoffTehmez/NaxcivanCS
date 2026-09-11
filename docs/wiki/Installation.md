# Quraşdırma və ilk işə salma

## Tələblər

| Alət | Repo əsası | İstifadə |
|---|---|---|
| Git | Versiya xüsusi pin edilməyib | Repo və Wiki tarixçəsi |
| .NET SDK | CI 8.0.x, layihələr net8.0 | C# build/test |
| .NET 8 runtime | net8.0 tətbiqlərinin icrası | Lokal oyun/backend |
| Godot .NET/Mono | 4.7.2 | Client və headless server |
| Git Bash | Windows shell skriptləri üçün | tools/*.sh |
| Docker | Opsional | İnfrastruktur sınaqları |
| PowerShell 7 | Sənəd/Wiki alətləri | tools/*.ps1 |

Godot versiyası csproj və CI ilə eyni olmalıdır. Bu tələb repodakı pin-dən gəlir; “ən yeni versiya” iddiası deyil. Başqa versiyaya keçid hər iki Godot layihəsi və CI/Docker pin-ləri birlikdə yoxlanılaraq edilməlidir.

## Repo və build

```bash
git clone https://github.com/MuradoffTehmez/NaxcivanCS.git
cd NaxcivanCS
git switch --detach 0.2.2
dotnet build NaxcivanCS.sln -c Release
dotnet test NaxcivanCS.sln -c Release --no-build
dotnet build server/NaxcivanCS.Server.csproj
dotnet build client/NaxcivanCS.Client.csproj
```

Teq checkout-u oxumaq/oynatmaq üçündür. Dəyişiklik hazırlamaq üçün [Development](Development.md) qaydasına uyğun budaq açın.

## Git Bash ilə sürətli oyun

```bash
bash tools/play.sh
bash tools/play.sh 2
```

İkinci əmr bir server və iki pəncərə açır. Skript localhost:27015 istifadə edir və bütün client-lər bağlananda öz server prosesini dayandırır. Eyni anda iki play.sh sessiyası açmaq port toqquşması yarada bilər.

Godot aşkarlanma ardıcıllığı GODOT_BIN, PATH-də godot, Windows winget qovluğu və adi Linux/macOS yollarıdır. Auto-discovery bir neçə versiya olduqda string sıralamasına əsaslana bilər; dəqiq executable göstərmək daha aydındır.

```bash
export GODOT_BIN='/c/Tools/Godot/Godot_v4.7.2-stable_mono_win64_console.exe'
bash tools/play.sh 2
```

## PowerShell ilə ayrıca başlatma

Aşağıdakı yolu öz Godot quraşdırmanıza uyğun dəyişin:

```powershell
$godotExe = 'C:/Tools/Godot/Godot_v4.7.2-stable_mono_win64_console.exe'
& $godotExe --headless --path server --import
& $godotExe --headless --path client --import
& $godotExe --headless --path server -- --port 27015
```

Başqa terminalda repo kökündən:

```powershell
$godotExe = 'C:/Tools/Godot/Godot_v4.7.2-stable_mono_win64_console.exe'
& $godotExe --path client -- --server 127.0.0.1 --port 27015 --name Tahmaz
```

## LAN bağlantısı

Server hostunun LAN IP-sini client-in --server arqumentinə verin. --port eyni olmalıdır; nəqliyyat UDP-dir. run-client.sh həmişə localhost istifadə etdiyi üçün LAN sınağında Godot executable-ını birbaşa çağırın.

Username trim-dən sonra 3–20 simvol olmalıdır. Bu, authenticated hesab deyil, sadəcə prototip görünən addır.

## Uğur əlamətləri

Server logunda 8 silah və 12 blockout blokunun yüklənməsi görünür. Client logunda qəbul edilmiş peer/komanda, HUD-da isə qoşulma vəziyyəti görünməlidir. Pəncərəyə klikdən sonra kursor tutulur.

[Problemlərin həlli](Troubleshooting.md) · [Testlər](Testing.md)
