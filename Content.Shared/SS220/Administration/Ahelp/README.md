# Как добавить смайлик в Ahelp

Сначала загрузи смайлик в Discord: для пересылки нужны его имя и ID.

1. Добавь PNG в `Resources/Textures/SS220/Interface/AhelpEmojis/`, например `my_emoji.png`.
2. Добавь запись в `Resources/Prototypes/SS220/Administration/ahelp_emojis.yml`:

```yaml
- type: ahelpEmoji
  id: my_emoji
  name: my_emoji
  discordId: "123456789012345678"
  texture: /Textures/SS220/Interface/AhelpEmojis/my_emoji.png
```

`id` — уникальный код для игры: латиница, цифры и подчёркивание.
`name` и `discordId` замени на настоящее имя и числовой ID смайлика Discord.

3. Передай обновлённые ресурсы серверу и клиенту и перезапусти их.
Смайлик появится в меню и будет доступен по коду `:my_emoji:`.
