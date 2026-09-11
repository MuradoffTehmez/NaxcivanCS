# Buraxılış proseduru

## Hazırlıq

Əvvəl git status, uzaq main və mənbə budağını yoxlayın. Dəyişikliklərdə istifadəçinin başqa işini itirməyin. Release versiyasını Directory.Build.props, GameConstants GameVersion/ContentVersion və hər iki project.godot faylında uyğunlaşdırın.

Wire format/mesaj dəsti üçün lazımdırsa ProtocolVersion artırılır. 0.2.2 protocol 2 istifadə edir. [Changelog](../CHANGELOG.md), [release qeydi](RELEASE-0.2.2.md), [known issues](KNOWN_ISSUES.md) və Wiki yenilənməlidir.

## Yoxlama və merge

Lokal [test planını](TESTING.md) icra edin. PR yaradıb eyni head commit-in CI nəticəsini gözləyin. Mövcud branch protection/review qaydalarını saxlayın. Main-ə merge-dən sonra merge commit üçün CI-ni də izləyin.

Annotated tag dəqiq release commit-ə qoyulur:

```bash
git fetch origin --tags
git switch main
git pull --ff-only
git tag -a 0.2.2 -m "NaxcivanCS 0.2.2 - gunplay and documentation"
git push origin refs/tags/0.2.2
```

Bu nümunə artıq main-ə birləşdirilmiş buraxılış üçündür. Eyni teq varsa onu silib yenidən yaratmayın. Uzaq tag peel SHA ilə main SHA-nın eyni olduğunu yoxlayın.

## GitHub Release

Tag, release title və qeydlər uyğun olmalıdır. Oyun prototip olduğu üçün pre-release statusu daha uyğun təsvir verir. Export edilmiş executable yoxlanmayıbsa hazır binary download vədi yazmayın.

## Wiki

Əsas repo və Wiki ayrı Git deposudur. İlk Wiki səhifəsi GitHub UI-də yaradılmalıdır. Sonra:

```powershell
pwsh -NoProfile -File tools/check-doc-links.ps1
pwsh -NoProfile -File tools/publish-wiki.ps1 -SourceRef 0.2.2
pwsh -NoProfile -File tools/publish-wiki.ps1 -SourceRef 0.2.2 -Publish
```

Export edilmiş səhifələri review edin. Publish fresh clone-da yalnız mənbə səhifələrini dəyişir və force-push etmir. Köhnə Wiki səhifələrini avtomatik silmir.

## Son təsdiq

Main uzaqla uyğun, working tree təmiz, teq düzgün SHA-da, CI uğurlu və Wiki Home/Sidebar linkləri açılan olmalıdır. Xarici nəşr bloklanarsa lokal hazırlığı və konkret bloklanmanı ayrıca hesabatda göstərin.
