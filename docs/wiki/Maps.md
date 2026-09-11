# Xəritələr və məkan dizaynı

## Hazırkı blockout

Client və server eyni BlockoutMap mənbəyindən dünya qurur. Client mesh/material və işıq əlavə edir; server statik kolliziya cisimləri yaradır. Bu, görünən örtüklə movement kolliziyasının mənbəsini birləşdirir. Hitscan divar yoxlaması isə ayrıca hələ çatışmır.

Döşəmə 40×40 vahiddir, xarici divarlar 4 vahid hündürlükdədir. Ümumilikdə 12 blok var: döşəmə, mərkəz örtüyü, A/B/C/D örtükləri, qərb/şərq qutuları və dörd xarici divar.

```text
                  North / Z=-20
                 Alpha Z=-12
      Cover_A                 Cover_C
  Crate_West       Mid        Crate_East
      Cover_D                 Cover_B
                 Bravo Z=+12
                  South / Z=+20
```

Sxem miqyaslı plan deyil; istiqamət və callout bələdçisidir. Alpha x başlanğıcı -4, hər oyunçu üçün artım 2 vahiddir; Bravo da eyni x qaydasını istifadə edir.

## Materiallar

Floor concrete, orta/divarlar stone, A–D wood, qutular metal kimi işarələnib. SurfaceMaterial hazırda metadata-dır; hər material üçün tamamlanmış footstep və penetration davranışı yoxdur.

## --map arqumenti

Server --map NC_Qala parametrini qəbul edib loga yazır, amma həmişə eyni BlockoutMap qurur. Başqa ad vermək real NC_Duzdag səhnəsini yükləmir. server_default.json-dakı map sahəsi də runtime loader-ə bağlı deyil.

## Planlaşdırılan xəritələr

| Ad | Tema | Dizayn niyyəti | Status |
|---|---|---|---|
| NC_Qala | Daş küçələr, qala memarlığı | Orta məsafə və balanslı marşrutlar | Konsept |
| NC_Duzdag | Duzdağ tunelləri | Yaxın/orta döyüş, şaqulilik | Konsept |
| NC_Araz | Anbar, dəmir yolu, cargo | Uzun sightline və sniper mövqeyi | Konsept |
| NC_Ordubad | Həyət və bağlar | Taktiki rotasiya | Konsept |
| NC_Batabat | Dağ və təbiət | Casual/Deathmatch məkanı | Konsept |

## Yeni xəritə hazırlamaq

Əvvəl graybox-da spawn təhlükəsizliyi, rotasiya vaxtı, sightline və giriş yolları ölçülməlidir. Sonra A/B site və callout-lar oyunçu testində yoxlanmalıdır. Art əlavə olunarkən hitbox/cover görünüşü ilə server kolliziyası uyğun saxlanmalıdır.

Final asset əlavə edilərkən mənbə və istifadə icazəsi qeyd olunmalıdır. Mövcud kommersiya xəritəsinin reproduksiyası layihə IP qaydasına uyğun deyil.

[Mənbə](../../shared/NaxcivanCS.Shared/Gameplay/BlockoutMap.cs) · [Assets](../../ASSETS.md)
