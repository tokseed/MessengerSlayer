# MessengerSlayer

Единая сборка проекта находится в `src/`: `Messenger.Client`, `Messenger.Server` и `Messenger.Shared`.

## Что собрано

- `Messenger.Client` — Avalonia UI-клиент: авторизация, регистрация, чаты, сообщения, профиль и вложения.
- `Messenger.Server` — TCP/TLS-сервер с Entity Framework Core и обработчиками авторизации, чатов и сообщений.
- `Messenger.Shared` — общие DTO, пакеты протокола и потоковый сериализатор.
- `scripts/` и SQL-файлы — материалы для инициализации базы данных.

## Запуск

Требуется .NET 10 SDK и доступный Microsoft SQL Server. Сервер создаёт базу `MessengerSlayer` автоматически через `EnsureCreated`.

Из каталога `check_folder/src`:

```bash
dotnet restore MessengerSlayer.slnx
dotnet run --project Messenger.Server/Messenger.Server.csproj
```

Во втором окне терминала:

```bash
dotnet run --project Messenger.Client/Messenger.Client.csproj -p:UsedAvaloniaProducts=
```

Клиент подключается к `localhost:5000`, использует TLS и сертификат `Messenger.Client/Certs/server.crt`.

Строка подключения находится в `src/Messenger.Server/appsettings.json`; при необходимости измените её перед запуском.

Для изменения адреса клиента отредактируйте `src/Messenger.Client/clientsettings.json`.

## Проверка сборки

В окружении, где Avalonia не может записать telemetry-файл, используйте:

```bash
dotnet build MessengerSlayer.slnx --no-restore -p:UsedAvaloniaProducts=
```
