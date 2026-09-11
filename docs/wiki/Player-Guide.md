# Oyunçu bələdçisi

## İlk duel

İki client ilə başladın. Hər pəncərənin statusunda serverə qoşulduğunu yoxlayın. Pəncərəyə klik edib mouse ilə ətrafa baxın. Alpha mənfi Z, Bravo müsbət Z tərəfində doğulur; başlanğıc baxışı mərkəzə yönəlir.

Bir maşında iki pəncərə hər iki oyunçunu eyni anda əl ilə idarə etmək üçün ideal deyil. Əsl duel üçün eyni LAN-dakı iki maşından istifadə edin. Lokal iki pəncərə bağlantı, remote model və atəş feedback-i sınağı üçün faydalıdır.

## İdarəetmə

| Giriş | Davranış |
|---|---|
| W/A/S/D | İrəli/sol/geri/sağ |
| Mouse | Yaw və pitch |
| Sol klik | Kursor sərbəstdirsə tutmaq; oyundaykən atəş |
| Sol klik basılı | AR-9 avtomatik atəş |
| R | Şarjor dolu deyilsə və reserve varsa reload |
| Space | Yerdən tullanmaq |
| Ctrl | Yerdə çömbəlmək |
| Shift | Yavaş hərəkət |
| Esc | Mouse-u sərbəst buraxmaq |

Kursor tutulmayanda normal gameplay üçün neytral input göndərilir. --auto-fire debug parametri bu qaydanın ayrıca test istisnasıdır. Tab scoreboard biti toplansa da bu build-də tam scoreboard ekranı yoxdur.

## HUD necə oxunur?

HP canı, armor zirehi, ping isə bağlantının RTT göstəricisini ifadə edir. Ammo şarjordakı və ehtiyat güllələrin sayıdır; reload müddətində RELOAD yazısı göstərilir. Crosshair recoil feedback-i verir, hitmarker server damage hadisəsinə əsaslanır.

Ammo HUD-u ilk spawn/respawn və packet loss vəziyyətində köhnə qala bilər. Belə uyğunsuzluq serverdə sonsuz ammo demək deyil: atəş qərarı serverin WeaponRuntime vəziyyətindən çıxır.

## Atəş və reload

AR-9 Qartal 30 gülləlik şarjor, 90 reserve və 2.4 saniyəlik reload konfiqurasiyası istifadə edir. Şarjor boşalanda reserve varsa reload avtomatik başlayır. Manual R qismən boş şarjoru doldurmağa imkan verir. Reload zamanı atəş yoxdur.

İlk atışın recoil sapması sıfırdır; davamlı atəş əvvəlcədən təyin edilmiş pattern üzrə sapır. Tətiki buraxmaq recoil-in bərpasına şərait yaradır. Hərəkətə görə random spread hələ faktiki güllə istiqamətinə əlavə edilməyib.

## Ölüm və təkrar spawn

Health sıfıra çatanda server ölümü qeydə alır. Təxminən 3 saniyə sonra can 100, armor 0 olur və silah doldurulur. Hazırkı testdə round sonunu gözləmək tələb olunmur.

## Faydalı test qaydası

Qısa atəş, uzun spray, manual reload və boş şarjor reload-unu ayrı-ayrı sınayın. Qarşı oyunçu hərəkətdə olarkən hitmarker və health dəyişməsini müşahidə edin. Sonra ping, teq və müşahidəni bug hesabatına yazın.

Örtük bu build-də hərəkət kolliziyası yaradır, lakin hitscan occlusion-u yoxdur. Buna görə divar arxasındakı vurulmanı competitive qayda kimi qəbul etməyin.

[Silahlar](Weapons.md) · [Damage](Damage-and-Armor.md) · [Dəstək](../../SUPPORT.md)
