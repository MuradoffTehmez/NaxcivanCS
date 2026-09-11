# Tez-tez verilən suallar və terminlər

## Suallar

**Oyun tam hazırdır?** Xeyr. 0.2.2 multiplayer gunplay prototipidir. Tam Bomb/Defuse, online hesablar və matchmaking yoxdur.

**8 silah varsa niyə yalnız birindən istifadə edirəm?** 8 silah config kataloqudur. Server hamıya weapon_rifle_01 verir; seçim/alış/inventory sistemi bağlanmayıb.

**Backend olmadan lokal oynamaq mümkündür?** Bəli. Cari client birbaşa dedicated serverə qoşulur. Backend heartbeat avtomatik göndərilmir.

**PostgreSQL və Redis məcburidirmi?** Lokal oyun və hazır API contract-ları üçün deyil. Compose gələcək inteqrasiya infrastrukturu verir.

**Niyə .NET Godot lazımdır?** Oyun script-ləri C#-dır. Adi Godot build-i həmin assembly-ləri işlətmək üçün uyğun runtime təqdim etmir.

**Niyə solution build kifayət etmir?** Root solution yalnız shared, backend və test layihələrini ehtiva edir. Godot client/server ayrıca csproj-dir.

**0.2.1 client qoşula bilər?** Protocol 1 istifadə edən client protocol 2 serverdə rədd edilir.

**Niyə teqdə v yoxdur?** Bu buraxılış üçün dəqiq tələb 0.2.2 olub. Əvvəlki teqlər tarixi adları ilə saxlanır.

**Shift həqiqətən səssiz hərəkətdirmi?** Sürəti azaldır. Footstep sistemi olmadığına görə ayrıca səssiz audio davranışı hələ tətbiq edilməyib.

**Bomba haradadır?** PRD və qaydalarda planlaşdırılıb; runtime objective yoxdur.

**Açıq mənbə lisenziyası hansıdır?** Repo hələ ayrıca açıq mənbə lisenziyası seçməyib. Bu sənədlər yeni lisenziya vermir.

## Terminlər

| Termin | İzah |
|---|---|
| Authority | Oyun nəticəsinə qərar verən tərəf |
| Dedicated server | Oyunçu render-i olmadan oyunu simulyasiya edən proses |
| Tick | Server simulyasiyasının bir addımı |
| Snapshot | Müəyyən tick-də oyunçu vəziyyətlərinin paketi |
| Prediction | Server cavabından əvvəl lokal hərəkətin proqnozu |
| Reconciliation | Lokal proqnozun server vəziyyəti ilə düzəldilməsi |
| Interpolation | İki məlum vəziyyət arasında görüntünün hesablanması |
| Lag compensation | Atəş hesabında hədəfin əvvəlki mövqeyinə baxılması |
| RTT / ping | Paketin gedib-qayıtma vaxtı |
| Hitscan | İstiqamət şüası ilə ani hit hesabı |
| Occlusion | Dünya obyektinin görmə/atəş xəttini bloklaması |
| Recoil | Atışlardan yaranan nişan sapması |
| Spread | Aim ətrafında dəqiqlik yayılması |
| Cadence / RPM | Atış tezliyi / dəqiqədə güllə |
| Reliable | Yenidən çatdırılma təmin edən nəqliyyat rejimi |
| Unreliable | Köhnə paketin yenidən ötürülmədiyi rejim |
| Graybox / blockout | Sadə həndəsə ilə ilkin xəritə |
| Soak test | Uzunmüddətli sabitlik sınağı |
| MR12 | Regulation yarısında 12 raund hədəfi |

[Home](Home.md) · [Game overview](Game-Overview.md)
