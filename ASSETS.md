# Asset və mənbə qeydləri

NaxcivanCS öz vizual kimliyini yaradan müstəqil layihədir. PRD-dəki IP qaydası Counter-Strike/Valve xəritə, model, audio, logo və decompiled kodunun köçürülməsini qəbul etmir.

## Hazırkı materiallar

| Material | Repo mənbəyi | Qeyd |
|---|---|---|
| Blockout xəritə | shared/NaxcivanCS.Shared/Gameplay/BlockoutMap.cs | Koddan qurulan prototip həndəsə |
| Silah view model-i | client/Scripts/Weapons/ViewModel.cs | Prosedural həndəsə |
| Tracer/impact | client/Scripts/Effects/ShotEffects.cs | Kodla yaradılan prototip effektlər |
| Ekran şəkilləri | docs/images/ | Əvvəlki inkişaf görüntüləri |
| Oyun icon-u | client/icon.svg | Mənşə/lisenziya yekun paylamadan əvvəl yoxlanmalıdır |
| Runtime asılılıqları | csproj, Dockerfile, CI | Öz upstream lisenziya və şərtlərinə tabedir |

Bu cədvəl tam üçüncü tərəf hüquq auditi deyil. Proqram təminatı və mənbə kodu [GPL-3.0-or-later](LICENSE) altında lisenziyalaşdırılır; oyun aktivləri və materialları isə müvafiq mənbə şərtlərinə tabedir.

## Yeni asset qəbul edilərkən

Fayl yolu, müəllif, mənbə URL-si, lisenziyanın adı/versiyası, əldə edilmə tarixi, tələb olunan attribution, edilmiş dəyişikliklər və paylama məhdudiyyətini qeyd edin. Mənbə məlum deyilsə release paketinə daxil etməyin.

Generated material üçün istifadə olunan aləti və yaradılma tarixini qeyd edin; bunun özü üçüncü tərəf hüquqlarının yoxlanmasını əvəz etmir. Oyun içində istifadə olunan adlar, xəritələr və loqolar da release inventarına daxil edilməlidir.
