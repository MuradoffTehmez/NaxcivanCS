# Təhlükəsizlik və etibar sərhədləri

## Authority modeli

Client input göndərir; server movement, ammo, recoil, hit və damage nəticəsinə qərar verir. Bu, nəticəni client-in özündən qəbul edən dizayndan daha məhdud etibar sərhədidir.

Ancaq server-authoritative adı tam təhlükəsizlik zəmanəti deyil. Paket formatı, float dəyərləri, sequence, vaxt büdcəsi və həyat dövrü ayrıca validasiya tələb edir. Hazır prototipdə bunların hamısı yekunlaşdırılmayıb.

## Mövcud müdafiələr

Protocol uyğunluğu, username uzunluğu, packet minimum uzunluqları, hərəkət oxu clamp, input queue limitləri və gameplay-in handshake-dən sonra qəbulu mövcuddur. Atəş tezliyi server WeaponRuntime intervalı ilə idarə olunur.

Movement yerdəyişməsi üçün ServerValidators yoxlaması var. SuspicionRules pozuntu çəkiləri, 80 review və 100 temporary restriction nəticəsi hesablayır. GameWorld hazırda bu action-u loga yazır; avtomatik tətbiq olunan tam moderation/ban mexanizmi kimi təqdim edilməməlidir.

## Mövcud sərhədlər

Backend heartbeat authentication və API rate limit tətbiq edilməyib. Username real hesab kimliyi deyil. Registry entry-si etibarlı server ownership sübutu daşımır. ContentVersion string-i asset imzası/hash yoxlaması deyil.

Divar occlusion və friendly fire boşluqları gameplay correctness riskləridir. Input limitinin ümumi simulyasiya vaxtına təsiri ayrıca təkmilləşdirilməlidir. Bunlar nəzarətli test mühitində prioritetləşdirilməlidir.

## Sirlər və məlumat

.env, private key, token və şəxsi oyunçu məlumatını repoya əlavə etməyin. Log hesabatlarında reproduksiya üçün lazım olmayan şəxsi məlumatı çıxarın. Gələcək account/stats sistemi üçün data retention və giriş icazələri ayrıca müəyyənləşdirilməlidir.

## Problem bildirmək

Məxfi zəiflik məlumatını açıq issue-ya yazmayın. Private vulnerability reporting aktivdirsə GitHub Security kanalından istifadə edin; əks halda maintainer-dən məxfi kanal istəyin. Təfərrüat və davranış qaydası [SECURITY.md](../../SECURITY.md)-dədir.

## Gələcək qəbul meyarı

Serverin etibarsız input altında çökməməsi, dəyişdirilmiş client-in vaxt/ammo səlahiyyətini genişləndirə bilməməsi, heartbeat sahibinin doğrulanması və məhdudiyyətlərin real icra olunması testlə göstərilməlidir.

[Analiz](../PROJECT_ANALYSIS.md) · [Known issues](../KNOWN_ISSUES.md)
