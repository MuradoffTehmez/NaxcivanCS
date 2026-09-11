# NaxcivanCS Wiki

**0.2.2 · Oyun, proqram təminatı və server idarəetməsi**

Bu Wiki NaxcivanCS-in necə oynandığını, necə qurulduğunu və hazırda hansı sərhədlərə malik olduğunu izah edir. Hədəf məhsul Naxçıvan mövzulu 5v5 taktiki FPS-dir; 0.2.2 build-i multiplayer gunplay prototipidir.

## Haradan başlamalı?

| Oxucu | Oxu ardıcıllığı |
|---|---|
| İlk dəfə oynayan | [Oyun haqqında](Game-Overview.md) → [Quraşdırma](Installation.md) → [Oyunçu bələdçisi](Player-Guide.md) |
| Gunplay/balansla maraqlanan | [Hərəkət](Movement.md) → [Silahlar](Weapons.md) → [Damage](Damage-and-Armor.md) |
| Proqramçı | [Development](Development.md) → [Arxitektura](Architecture.md) → [Client](Client.md) / [Server](Dedicated-Server.md) |
| Network proqramçısı | [Protokol](Network-Protocol.md) → [Prediction və lag compensation](Netcode.md) |
| Server operatoru | [Dedicated server](Dedicated-Server.md) → [Konfiqurasiya](Configuration.md) → [Əməliyyatlar](Operations.md) |
| Contributor | [Testlər](Testing.md) → [Töhfə](../../CONTRIBUTING.md) → [Release](Release-Workflow.md) |

## Məzmun xəritəsi

### Oyun

- [Vizyon və hazır funksiyalar](Game-Overview.md)
- [İdarəetmə və ilk duel](Player-Guide.md)
- [Hərəkət, kamera və nişan](Movement.md)
- [Silah kataloqu, ammo, recoil](Weapons.md)
- [Damage, armor və hit qeydiyyatı](Damage-and-Armor.md)
- [Raund, bomba və economy hədəfləri](Game-Modes-and-Economy.md)
- [Blockout xəritə və gələcək xəritələr](Maps.md)

### Proqram təminatı

- [Quraşdırma](Installation.md), [development workflow](Development.md)
- [Arxitektura](Architecture.md), [client](Client.md), [dedicated server](Dedicated-Server.md)
- [Wire protokolu](Network-Protocol.md), [netcode](Netcode.md)
- [Backend API nümunələri](Backend-API.md), [konfiqurasiya](Configuration.md)
- [UI, audio və localization](UI-Audio-and-Localization.md)
- [Testlər](Testing.md), [server əməliyyatları](Operations.md)
- [Təhlükəsizlik sərhədləri](Security-and-Trust.md), [release/Wiki workflow](Release-Workflow.md)
- [Problemlərin həlli](Troubleshooting.md), [FAQ və terminlər](FAQ-and-Glossary.md)
- [Yol xəritəsi və layihə analizi](Roadmap-and-Analysis.md)

## Statusları necə oxumalı?

**Tətbiq edilib** kodun işlək runtime axınına qoşulduğunu bildirir. **Qayda/skelet** model və ya test olunan helper olduğunu, lakin tam oyuna bağlanmadığını bildirir. **Plan** PRD hədəfidir. Avtomatik test, kod baxışı və manual test eyni sübut səviyyəsi deyil.

Build/game/content: 0.2.2. Git teqi: 0.2.2. Protocol: 2. Köhnə protocol client ilə serverə qoşulma qəbul edilmir.

Wiki mənbəyi əsas repoda docs/wiki qovluğudur. Mənbəyə keçidlər nəşr zamanı 0.2.2 teqinə bağlanır; Wiki tarixçəsi əsas Git tarixçəsindən ayrıdır.

[README](../../README.md) · [Buraxılış qeydləri](../RELEASE-0.2.2.md) · [Məlum problemlər](../KNOWN_ISSUES.md)
