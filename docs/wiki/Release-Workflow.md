# Buraxılış və Wiki nəşri

## 0.2.2 versiya xəritəsi

| Mənbə | Dəyər |
|---|---|
| Git tag | 0.2.2 |
| Directory.Build.props | 0.2.2 |
| GameConstants.GameVersion | 0.2.2 |
| GameConstants.ContentVersion | 0.2.2 |
| Client/server config/version | 0.2.2 |
| ProtocolVersion | 2 |

Əvvəlki teqlər v0.1.0, v0.2.0 və v0.2.1-dir. Yeni teqin dəqiq adı istifadəçinin istədiyi 0.2.2-dir. ProtocolVersion müstəqil integer-dir; content hash yoxlaması deyil.

## Release addımları

Əvvəl mənbə budağını main ilə müqayisə edin, build/test və CI nəticəsini yoxlayın. Sonra reviewed release dəyişikliklərini main-ə merge edin, annotated tag yaradın və konkret main/tag ref-lərini push edin. Mövcud teq overwrite edilməməlidir.

Changelog gameplay dəyişikliklərini, release hesabatı real yoxlama sübutlarını, known issues isə açıq məhdudiyyətləri saxlayır. Release title-da “stable production” kimi təsdiqlənməmiş iddia yazmayın.

## Wiki ayrı repodur

GitHub Wiki əsas repository-nin docs qovluğunu avtomatik göstərmir. Onun Git ünvanı NaxcivanCS.wiki.git-dir. İlk səhifə GitHub UI-də yaradılmayıbsa clone “Repository not found” verə bilər.

İlk Home səhifəsi saxlandıqdan sonra:

```powershell
pwsh -NoProfile -File tools/publish-wiki.ps1 -SourceRef 0.2.2
pwsh -NoProfile -File tools/publish-wiki.ps1 -SourceRef 0.2.2 -Publish
```

Birinci əmr artifacts/wiki altında nəşrə hazır faylları yaradır. İkinci əmr ayrıca Wiki clone-u yaradıb faylları commit/push edir. Default publish açıq deyil; -Publish tələb olunur.

## Link strategiyası

Mənbə səhifələri local .md linkləri istifadə edir. Export zamanı Wiki-daxili linklər GitHub Wiki URL-sinə çevrilir, repo mənbələrinə linklər SourceRef teqindəki blob/tree URL-sinə bağlanır. _Sidebar və _Footer nəşrdə naviqasiyanı təmin edir.

Əvvəlki, artıq mənbədə olmayan Wiki səhifələri avtomatik silinmir. Belə silinmə ayrıca review tələb edən bakım qərarıdır.

## Dəyişiklik baxımı

İlk növbədə docs/wiki mənbəyini redaktə edin və PR-də review edin. Wiki UI-də təcili düzəliş edilərsə eyni düzəlişi əsas repoya köçürün ki, növbəti nəşr onu geri çevirməsin.

[Tam release proseduru](../RELEASING.md) · [Changelog](../../CHANGELOG.md)
