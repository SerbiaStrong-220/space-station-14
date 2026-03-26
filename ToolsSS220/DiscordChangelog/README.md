## Описание
Это скрипт для извлечения чейнджлога из ПРа, изображений и видео связанных с ним, и отправки его на определённый дискорд-канал.

## Требования

Node.js 22.13.+

## Использование

Для установки зависимостей откройте консоль в текущей директории и выполните команду: `npm install`

Скрипт содержит несколько инпутов:
- `WEBHOOK_TOKEN` (обязательный) - токен дискорд-вебхука в формате `{id}/{token}` (берётся из его url: `https://discord.com/api/webhooks/{id}/{token}`);
- `GITHUB_TOKEN` (обязательный) - токен доступа к репозиторию. При выполнении скрипта через github workflow можно использовать автогенерируемый токен (`secrets.GITHUB_TOKEN`), при самостоятельном выполнении через консоль необходимо сгенерировать и использовать [личный токен](https://docs.github.com/ru/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens);
- `PULL_URL` (обязательный) - API-url целевого ПРа (пример: `https://api.github.com/repos/SerbiaStrong-220/space-station-14/pulls/3845`);
- `WEBHOOK_USERNAME` (необязательный) - имя вебхука, отображаемое в дискорде;
- `WEBHOOK_AVATAR_URL` (необязательный) - аватар вебхука, отображаемый в дискорде;

Запускать скрипт необходимо с сетом обязательных инпутов в environment.

Пример в PowerShell:
```
$env:WEBHOOK_TOKEN="{дискорд-токен}"
$env:GITHUB_TOKEN="{гитхаб-токен}"
$env:PULL_URL="{url}"
node index.js
```
или в одну строку:
```
$env:WEBHOOK_TOKEN="{дискорд-токен}"; $env:GITHUB_TOKEN="{гитхаб-токен}"; $env:PULL_URL="{url}"; node index.js
```
