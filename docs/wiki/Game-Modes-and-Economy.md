# Oyun rejimləri, raund və economy

> Gameplay/API nümunələri 0.2.2 tarixi snapshot-ına aiddir. Cari stable və unreleased dəyişikliklər üçün [README](../../README.md) və [CHANGELOG](../../CHANGELOG.md) əsasdır.

**Bu səhifədəki Bomb/Defuse və economy bölmələri məhsul hədəfi və shared qaydalardır. Tam oynanıla bilən inteqrasiya 0.2.2-də yoxdur.**

## Hədəf: 5v5 Bomb/Defuse

Alpha hücum, Bravo müdafiə rolunu təmsil edir. Məqsəd bomba yerləşdirmə/zərərsizləşdirmə və komanda eliminasiya şərtləri ilə round qazanmaqdır. Hazırkı server komanda bölgüsü və canlı döyüş verir, amma objective və round winner lifecycle-ı yoxdur.

| Hədəf parametr | Default |
|---|---:|
| Oyunçu/komanda | 5 |
| Maksimum oyunçu | 10 |
| Half | 12 raund |
| Regulation qələbə | 13 |
| Overtime half | 3 raund |
| Freeze | 10 s |
| Buy | 20 s |
| Active | 105 s |
| Bomb | 40 s |
| Round-end gecikməsi | 5 s |
| Plant | 3.2 s |
| Defuse / kit ilə | 10 / 5 s |

MatchRules sadə winner/overtime/side-swap helper-ləri saxlayır. Tam overtime state machine və competitive düzgünlük ayrıca inteqrasiya və test tələb edir. Bu sabitlər hazır match serverinin bütün davranışı kimi təqdim edilməməlidir.

## RoundController skeleti

Warmup → FreezeTime → BuyTime → Active → RoundEnd keçidləri var; BombPlanted ayrıca fazadır. ServerMain bu node-u cari dünyaya əlavə etmir. Taymerin bitməsi özlüyündə tam victory hesablaması və inventory reset yaratmır.

## Economy qaydaları

| Qayda | Məbləğ |
|---|---:|
| Start money | 800 |
| Maksimum money | 16000 |
| Round win | 3250 |
| Loss bonus | 1400 / 1900 / 2400 / 2900 / 3400 |
| Plant team | 800 |
| Plant player | 300 |
| Defuse player | 300 |
| Bomb exploded team sabiti | 3500 |

EconomyRules loss ladder, clamp və TryPurchase helper-lərini verir. Alış üçün məbləğin çatması və qiymətin mənfi olmaması yoxlanır. Lakin server BuyRequest handler-i, buy zone, inventory və nəticə yayımı hələ yoxdur.

## Gələcək rejimlər

Casual, Deathmatch, Training və bot sistemi PRD-də planlaşdırılıb. Hazırkı respawn sınağı bunların tam tətbiq edilmiş versiyası deyil. Son balans rəqəmləri playtest nəticələrinə görə dəyişə bilər.

## Qəbul meyarı

Tam Bomb Mode üçün iki komanda qoşulmalı, round başlamalı, alış serverdə təsdiqlənməli, bomba yalnız qanuni sahədə yerləşməli, victory və economy hesablanmalı, növbəti raund inventar/health/reset ilə başlamalıdır. Hər keçid disconnect, death və vaxt bitməsi ilə sınanmalıdır.

[Roadmap](Roadmap-and-Analysis.md) · [PRD](../PRD.md)
