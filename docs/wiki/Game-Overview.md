# Oyun haqqında

> Gameplay/API nümunələri 0.2.2 tarixi snapshot-ına aiddir. Cari stable və unreleased dəyişikliklər üçün [README](../../README.md) və [CHANGELOG](../../CHANGELOG.md) əsasdır.

## NaxcivanCS nədir?

NaxcivanCS rəqabətli komanda oyunu qurmaq üçün hazırlanmış müstəqil taktiki first-person shooter layihəsidir. Xəritə və vizual kimlik üçün Naxçıvanın qalaları, daş küçələri, Duzdağ, Araz ətrafındakı sənaye məkanları və Ordubad həyətləri ilham mənbəyidir. Bunlar hazır asset paketi deyil, məhsulun yaradıcı istiqamətidir.

Əsas vizyon aşağı giriş baryeri, aydın oyunçu görünüşü, öyrənilə bilən recoil və nəticəyə serverin qərar verməsidir. Oyunçu bacarığı nişan, mövqe seçimi, komanda ünsiyyəti və gələcək economy qərarları ilə fərqlənməlidir.

## Bu build-də real oyun dövrəsi

1. Dedicated server açılır.
2. Client-lər həmin IP/UDP porta qoşulur.
3. Server onları Alpha və Bravo komandalarına növbə ilə bölür.
4. Hər oyunçu AR-9 Qartal ilə blockout xəritədə spawn olur.
5. Oyunçu hərəkət edib atəş açır; server ammo, recoil, hit və damage hesablayır.
6. Ölən oyunçu təxminən 3 saniyə sonra yenidən spawn olur.

Bu dövrə gunplay və şəbəkə təməlini sınaqdan keçirir. Qalib komandanın elan edildiyi tam match, bomb site və alış mərhələsi hələ yoxdur. Avtomatik respawn olması ayrıca tamamlanmış Deathmatch rejiminin mövcudluğu demək deyil.

## Məhsul prinsipləri

| Prinsip | Mənası | Hazır vəziyyət |
|---|---|---|
| Competitive First | Əsas mexanika və fairness prioritetdir | Core prototip qurulur |
| Server Authority | Kill/damage/ammo nəticəsini server hesablayır | Əsas axında tətbiq edilib |
| No Pay-to-Win | Satınalma güc üstünlüyü verməməlidir | Monetizasiya sistemi yoxdur |
| Clear Visibility | Oyunçu fon içində itmir | Sadə material və rəngli modellər |
| Performance First | Yüngül, ölçülən simulyasiya | Tick hədəfi var, geniş benchmark yoxdur |

## Nə gözləməməli?

0.2.2-də hesab yaratma ekranı, matchmaking düyməsi, rank, store, friends/party, səsli chat, bot və final xəritələr yoxdur. PRD-dəki minimum PC və FPS rəqəmləri ölçülmüş bu build zəmanəti kimi qəbul edilməməlidir.

Şəkillər erkən prototipi göstərir. Son görünüş, audio, silah animasiyaları və oyun balansı dəyişəcək. Əsas məlum texniki problem divarların hazırkı hitscan yolunda gülləni bloklamamasıdır; ciddi taktiki örtük testi üçün əvvəl bu boşluq aradan qaldırılmalıdır.

[İlk duel](Player-Guide.md) · [Gələcək rejimlər](Game-Modes-and-Economy.md) · [Məhsul PRD-si](../PRD.md)
