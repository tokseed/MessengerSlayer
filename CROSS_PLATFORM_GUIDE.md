# MessengerSlayer — кроссплатформенный запуск

Эта версия построена по правилу:

- `Messenger.Server` — исходный код коллеги, не изменён;
- `Messenger.Shared` — исходный код коллеги, не изменён;
- код БД, SQL-файлы и серверные конфиги коллеги — не изменены;
- наш актуальный Avalonia-клиент находится в `src/Messenger.Client`;
- кроссплатформенность добавлена новыми файлами вокруг проекта.

## Что нужно установить

На любой ОС:

1. Git — только если работаешь с репозиторием.
2. .NET SDK 10.
3. Docker с поддержкой `docker compose`.

На Windows удобно использовать Docker Desktop.
На Linux достаточно Docker Engine + Compose plugin.
На macOS — Docker Desktop/совместимый Docker runtime.

## База данных

Одинаковая dev-БД для Windows/Linux/macOS:

```bash
docker compose up -d sqlserver
```

Используется SQL Server 2022 Developer в контейнере.

Значения по умолчанию:

```text
Host: localhost
Port: 1433
Database: MessengerSlayer
Login: sa
Password: Your_password123
```

Они совпадают с исходным `appsettings.json` сервера коллеги.

Данные сохраняются в Docker volume:

```text
messengerslayer_sql_data
```

Обычный `docker compose down` данные НЕ удаляет.

### Свои порт/пароль

Не редактируй сервер коллеги.

При необходимости скопируй:

```text
.env.example
```

в:

```text
.env
```

и измени локальные значения.

`docker compose` возьмёт их автоматически, а наши `run-server.ps1` / `run-server.sh`
передадут серверу connection string через переменную окружения:

```text
ConnectionStrings__DefaultConnection
```

`Program.cs` и `appsettings.json` при этом остаются нетронутыми.

Не коммить настоящий `.env`.

## Windows — первый запуск

Из корня проекта:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\crossplatform\setup.ps1
```

Это:

1. поднимет SQL Server в Docker;
2. дождётся готовности контейнера;
3. выполнит restore;
4. соберёт Debug;
5. соберёт Release.

Затем в первом терминале:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\crossplatform\run-server.ps1
```

Во втором терминале:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\crossplatform\run-client.ps1
```

Для второго клиента просто ещё раз запусти `run-client.ps1`.

## Linux — первый запуск

Один раз после распаковки ZIP:

```bash
chmod +x ./scripts/crossplatform/*.sh
```

Затем:

```bash
./scripts/crossplatform/setup.sh
```

Сервер:

```bash
./scripts/crossplatform/run-server.sh
```

В другом терминале клиент:

```bash
./scripts/crossplatform/run-client.sh
```

## macOS

Команды такие же, как на Linux:

```bash
chmod +x ./scripts/crossplatform/*.sh
./scripts/crossplatform/setup.sh
./scripts/crossplatform/run-server.sh
./scripts/crossplatform/run-client.sh
```

SQL Server container использует:

```yaml
platform: linux/amd64
```

поэтому на Apple Silicon Docker будет использовать x86-64 эмуляцию. Это dev-вариант,
а не оптимизированный production deployment.

## Просто собрать без Docker

Windows:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\crossplatform\build.ps1
```

Linux/macOS:

```bash
./scripts/crossplatform/build.sh
```

## Остановить контейнер БД

Windows:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\crossplatform\db-down.ps1
```

Linux/macOS:

```bash
./scripts/crossplatform/db-down.sh
```

Это не удаляет volume и данные.

## Сделать готовые self-contained сборки

Windows:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\crossplatform\publish-all.ps1
```

Linux/macOS:

```bash
./scripts/crossplatform/publish-all.sh
```

Результат:

```text
publish/
├── win-x64/
│   ├── client/
│   └── server/
├── win-arm64/
├── linux-x64/
├── linux-arm64/
├── osx-x64/
└── osx-arm64/
```

Каждый каталог содержит self-contained приложение, поэтому соответствующей машине
не требуется отдельно установленный .NET Runtime.

## Важное ограничение этой версии

Мы намеренно не меняли сервер/Shared коллеги.

Из-за этого прежние серверные проблемы, которые раньше временно исправлялись нашей
интеграционной версией, могут снова проявиться. Например, если два клиента снова
начнут отключаться.

В таком случае сначала фиксируем конкретную ошибку и согласовываем минимальный diff
с владельцем серверного кода. Никаких скрытых замен целых серверных файлов.

Email/аватар в новом UI также не записываются в SQL, если существующий протокол
коллеги не предоставляет API записи этих полей. Клиент не подменяет это серверной
реализацией.
