# MessengerSlayer

Учебный TCP-мессенджер на C#/.NET с графическим клиентом Avalonia, TLS-соединением и SQL Server.

## Возможности

| Компонент | Назначение |
|---|---|
| `Messenger.Client` | Регистрация, вход, чаты, сообщения, профиль и вложения |
| `Messenger.Server` | TCP/TLS-сервер, авторизация, чаты и сохранение сообщений |
| `Messenger.Shared` | Общие модели, пакеты протокола и сериализация |
| SQL Server | Пользователи, чаты, участники и история сообщений |

## Структура проекта

```text
check_folder/
├── build-and-run.bat       # Сборка и запуск Windows
├── build-and-run.sh        # Сборка и запуск macOS/Linux
├── CompileDB.sql            # SQL-схема
├── scripts/
│   └── upgrade_add_username.sql
└── src/
    ├── Messenger.Client/
    ├── Messenger.Server/
    ├── Messenger.Shared/
    └── MessengerSlayer.slnx
```

## Требования

- .NET SDK 10;
- Docker Desktop и контейнер Microsoft SQL Server;
- macOS, Linux или Windows;
- для клиента — графическая среда рабочего стола.

Проверка .NET:

```bash
dotnet --version
```

## Запуск базы данных

Если контейнер уже создан:

```bash
docker start sqlserver_container
```

Проверить состояние:

```bash
docker ps
```

Должен быть опубликован порт `1433`:

```text
0.0.0.0:1433->1433/tcp
```

Сервер использует базу `MessengerSlayer` и создаёт её автоматически при первом запуске.

Параметры подключения находятся в `src/Messenger.Server/appsettings.json`.

## Запуск на macOS/Linux

Из каталога `check_folder`:

```bash
chmod +x build-and-run.sh
./build-and-run.sh
```

Скрипт восстановит зависимости, соберёт решение, запустит сервер и клиент.

Запустить только сервер:

```bash
./build-and-run.sh server
```

Запустить только клиент:

```bash
./build-and-run.sh client
```

## Запуск на Windows

Открой PowerShell или CMD в каталоге `check_folder`:

```powershell
.\build-and-run.bat
```

Только сервер:

```powershell
.\build-and-run.bat server
```

Только клиент:

```powershell
.\build-and-run.bat client
```

## Запуск вручную

Сначала запусти сервер:

```bash
cd src
dotnet restore MessengerSlayer.slnx
dotnet build MessengerSlayer.slnx --no-restore -m:1 -p:UsedAvaloniaProducts=
dotnet run --project Messenger.Server/Messenger.Server.csproj --no-build
```

Ожидаемый вывод:

```text
Certificate loaded: CN=localhost
Server started on port 5001 (TLS enabled)
```

Во втором терминале запусти клиент:

```bash
cd src
dotnet run --project Messenger.Client/Messenger.Client.csproj --no-build -p:UsedAvaloniaProducts=
```

## Настройка клиента

Клиент читает настройки из `src/Messenger.Client/clientsettings.json`.

Для запуска на том же компьютере:

```json
{
  "host": "localhost",
  "port": 5001,
  "useTls": true,
  "pinnedCertificatePath": "Certs/server.crt",
  "chatSyncIntervalMilliseconds": 1500
}
```

Порядок запуска: SQL Server → Messenger.Server → клиент.

## Подключение другого устройства

Для устройств в одной сети укажи в `clientsettings.json` локальный IP компьютера с сервером:

```bash
ipconfig getifaddr en0
```

Для подключения через интернет нужен внешний IP и проброс TCP-порта `5001` на роутере:

```text
TCP внешний порт 5001 → локальный IP компьютера:5001
```

На клиенте друга укажи:

```json
{
  "host": "ВНЕШНИЙ_IP_СЕРВЕРА",
  "port": 5001,
  "useTls": true,
  "pinnedCertificatePath": "Certs/server.crt"
}
```

Проверка порта с Windows:

```powershell
Test-NetConnection ВНЕШНИЙ_IP_СЕРВЕРА -Port 5001
```

Результат должен содержать `TcpTestSucceeded : True`.

Если порт недоступен, проверь firewall macOS, проброс порта на роутере и отсутствие CGNAT у провайдера. Порт SQL Server `1433` нельзя открывать в интернет.

## Обновление существующей базы

Если в старой базе отсутствует поле `username`, выполни в SQL Server файл `scripts/upgrade_add_username.sql`.

Сервер также проверяет наличие этой колонки при запуске и добавляет её автоматически.

## Проверка сборки

```bash
cd src
dotnet build MessengerSlayer.slnx --no-restore -m:1 -p:UsedAvaloniaProducts=
```

Успешный результат — `Build succeeded` и `0 Error(s)`.

## Остановка

В окне сервера нажми `Ctrl+C`.

При необходимости остановить базу:

```bash
docker stop sqlserver_container
```
