# MessengerSlayer — Конспект логики проекта

## Архитектура

Проект построен по схеме **three-tier**: Client → Shared ← Server.

```
Client ──→ Shared ←── Server
(UI)     (контракт)   (логика + БД)
```

- **Messenger.Shared** — общая библиотека (модели, пакеты, сериализация, хеширование)
- **Messenger.Server** — TCP сервер, БД, бизнес-логика
- **Messenger.Client** — TCP клиент, сервисные обёртки, консольный интерфейс

Клиент и сервер **не знают** друг о друга напрямую. Они общаются через Shared — общий язык (пакеты).

---

## Протокол обмена данными

### Формат пакета

Каждый пакет передаётся в формате:

```
[4 байта: длина payload][UTF-8 payload]
```

Payload:
```
TypeName\n{json}
```

- **TypeName** — имя класса пакета (например `AuthPacket`)
- **json** — JSON-сериализация объекта (camelCase)
- **Max packet size** — 1 MB

### Пример

Отправка AuthPacket:
```
4 bytes: [0x00, 0x00, 0x00, 0x3A] (длина = 58)
UTF-8:   AuthPacket\n{"username":"test","passwordHash":"123456"}
```

### PacketSerializer

Статический класс в `Messenger.Shared/Network/PacketSerializer.cs`:

- `Serialize(Packet)` → byte[] — сериализует пакет в байты
- `Deserialize(byte[])` → Packet? — десериализует байты в пакет
- `SendAsync(Stream, Packet)` — отправляет пакет в поток
- `ReceiveAsync(Stream)` — читает пакет из потока (с обработкой частичных чтений)

Для разрешения типов используется `Type.GetType("Messenger.Shared.Packets.{typeName}")`.

---

## Типы пакетов (PacketType enum)

| Тип | Направление | Данные |
|-----|-------------|--------|
| `Auth` | Клиент → Сервер | Username, PasswordHash |
| `AuthResponse` | Сервер → Клиент | Success, UserId, Error |
| `Register` | Клиент → Сервер | Username, PasswordHash, FirstName, LastName, PhoneNumber |
| `RegisterResponse` | Сервер → Клиент | Success, UserId, Error |
| `Message` | Клиент ↔ Сервер | SenderId, ChatId, Content, ReplyToMessageId? |
| `MessageAck` | Сервер → Клиент | Success, MessageId, Error |
| `ChatCreate` | Клиент → Сервер | Title, ChatType, ParticipantIds |
| `ChatCreateResponse` | Сервер → Клиент | Success, ChatId, Error |
| `ChatListRequest` | Клиент → Сервер | UserId |
| `ChatListResponse` | Сервер → Клиент | List\<ChatDto\> |
| `UserListRequest` | Клиент → Сервер | (пустой) |
| `UserListResponse` | Сервер → Клиент | List\<UserDto\> |
| `MessageHistoryRequest` | Клиент → Сервер | ChatId, Limit |
| `MessageHistoryResponse` | Сервер → Клиент | List\<MessageDto\> |
| `Disconnect` | Клиент ↔ Сервер | (пустой) |

### Жизненный цикл пакета

```
Клиент                          Сервер
  │                               │
  ├── AuthPacket ────────────────►│
  │                               ├── AuthService.AuthenticateAsync()
  │                               │   ├── SELECT user WHERE username = ?
  │                               │   ├── PasswordHasher.Verify(password, hash)
  │                               │   └── UPDATE user SET last_seen_at = NOW
  │◄── AuthResponsePacket ───────┤
  │                               │
  ├── MessagePacket ─────────────►│
  │                               ├── MessageService.SendMessageAsync()
  │                               │   ├── Проверка membership в чате
  │                               │   ├── INSERT INTO messages
  │                               │   └── BroadcastMessageAsync()
  │                               │       ├── SELECT chat_member_ids WHERE chat_id = ?
  │                               │       └── Send MessagePacket каждому участнику
  │◄── MessageAckPacket ─────────┤
  │                               │
  ├── DisconnectPacket ──────────►│
  │                               └── Закрытие соединения
```

---

## Безопасность

### TLS (Transport Layer Security)

Все TCP соединения обёрнуты в `SslStream`. Протоколы: TLS 1.2, TLS 1.3.

**Сервер:**
- Загружает самоподписанный сертификат `.pfx`
- `AuthenticateAsServerAsync()` после TCP accept
- Все данные передаются в зашифрованном виде

**Клиент:**
- `AuthenticateAsClientAsync()` после TCP connect
- Для разработки принимает самоподписанные сертификаты (`ValidateServerCertificate` → true)

**Порядок handshake:**
```
Клиент                          Сервер
  │                               │
  ├── TCP connect ───────────────►│
  ├── TLS ClientHello ──────────►│
  │◄── TLS ServerHello + cert ──┤
  ├── TLS Finished ─────────────►│
  │                               │
  │  (все данные зашифрованы)     │
```

### Хеширование паролей (PBKDF2)

Пароли **никогда** не хранятся в открытом виде.

- Алгоритм: PBKDF2 (RFC 2898)
- Хеш-функция: SHA-256
- Итерации: 100,000
- Размер соли: 16 байт
- Размер хеша: 32 байта

**Формат хранения:** `{base64(salt)}.{base64(hash)}`

**Регистрация:**
1. Клиент отправляет plain text пароль
2. Сервер генерирует случайную соль
3. Сервер вычисляет хеш через PBKDF2
4. Сервер сохраняет `{salt}.{hash}` в БД

**Логин:**
1. Клиент отправляет plain text пароль
2. Сервер находит пользователя в БД
3. Сервер извлекает соль из `{salt}.{hash}`
4. Сервер вычисляет хеш от введённого пароля с той же солью
5. Сервер сравнивает хеши через `CryptographicOperations.FixedTimeEquals()` (constant-time)

---

## Messenger.Server

### Точка входа (Program.cs)

```
1. Чтение appsettings.json
2. Загрузка сертификата (.pfx)
3. Создание EF Core DbContext
4. EnsureCreatedAsync() — автоматическое создание БД
5. Создание сервисов (AuthService, MessageService, ChatService)
6. Создание TcpServer
7. Запуск accept loop
8. Обработка Ctrl+C для graceful shutdown
```

### Network/TcpServer

Отвечает за:
- Прослушивание TCP порта (`IPAddress.Any`)
- Приём новых подключений (`AcceptTcpClientAsync`)
- Обёртку соединения в `SslStream` (TLS handshake)
- Создание `ClientHandler` для каждого клиента
- Поддержание списка активных клиентов (thread-safe)
- **Broadcast** — рассылка пакетов участникам чата

**BroadcastToChatAsync(chatId, packet, excludeUserId):**
```
1. SELECT user_id FROM chat_members WHERE chat_id = ?
2. Найти в списке клиентов тех, чей CurrentUserId в списке участников
3. Исключить отправителя (excludeUserId)
4. Отправить пакет каждому найденному клиенту
```

### Network/ClientHandler

Отвечает за:
- Чтение пакетов от клиента (read loop)
- Маршрутизацию пакетов (switch по PacketType)
- Вызов соответствующих сервисов
- Отправку ответа клиенту
- Вызов broadcast после отправки сообщения

**Обработчики пакетов:**

| Пакет | Метод | Логика |
|-------|-------|--------|
| Auth | HandleAuthAsync | Вызов AuthService.AuthenticateAsync, установка _currentUserId |
| Register | HandleRegisterAsync | Вызов AuthService.RegisterAsync |
| Message | HandleMessageAsync | SendMessageAsync + BroadcastToChatAsync |
| ChatCreate | HandleChatCreateAsync | Автоматическое добавление текущего пользователя в участники |
| ChatListRequest | HandleChatListAsync | GetChatsAsync |
| UserListRequest | HandleUserListAsync | GetUsersAsync |
| MessageHistoryRequest | HandleMessageHistoryAsync | GetMessagesAsync |
| Disconnect | HandleDisconnect | Закрытие соединения |

### Services/AuthService

- `AuthenticateAsync(username, password)` — поиск пользователя, верификация пароля через PBKDF2
- `RegisterAsync(username, password, firstName, lastName, phoneNumber)` — проверка уникальности, хеширование пароля, создание пользователя
- `GetUsersAsync()` — список всех пользователей (для UI)

### Services/MessageService

- `SendMessageAsync(senderId, chatId, content, replyToMessageId)` — проверка membership, вставка в БД
- `GetMessagesAsync(chatId, limit)` — последние N сообщений чата

### Services/ChatService

- `CreateChatAsync(title, chatType, participantIds)` — создание чата + добавление участников
- `GetChatsAsync(userId)` — список чатов пользователя
- `GetChatMemberIdsAsync(chatId)` — список ID участников (для broadcast)

### Database

**Entities (EF Core):**

| Entity | Таблица | Ключ |
|--------|---------|------|
| User | users | Id (int) |
| Chat | chats | Id (int) |
| ChatMember | chat_members | (UserId, ChatId) — составной |
| Message | messages | Id (int) |

**Связи:**
- ChatMember → User (many-to-one)
- ChatMember → Chat (many-to-one)
- Message → Chat (many-to-one)
- Message → User/ Sender (many-to-one)

**Mappings (OnModelCreating):**
- Все колонки маппятся на snake_case имена в SQL
- Типы соответствуют SQL схеме (nvarchar, datetime2, bit)

---

## Messenger.Client

### Network/TcpClientService

Отвечает за:
- Подключение к серверу по TCP
- TLS handshake (`AuthenticateAsClientAsync`)
- Фоновый поток чтения входящих пакетов (`ListenAsync`)
- `SendAsync(packet)` — отправка пакета
- `SendAndWaitAsync(packet)` — отправка + ожидание ответа (10 сек таймаут)

**SendAndWaitAsync:**
```
1. Создать TaskCompletionSource<Packet>
2. Подписаться на OnPacketReceived
3. Отправить пакет
4. Запустить таймер 10 сек
5. Ждать ответ или таймаут
6. При таймауте — TrySetCanceled + отписка
```

### Services

Обёртки над пакетами для удобства использования UI-шником:

**AuthService:**
- `LoginAsync(username, password)` → AuthResponsePacket
- `RegisterAsync(username, password, firstName, lastName, phoneNumber)` → RegisterResponsePacket
- `GetUsersAsync()` → List\<UserDto\>

**MessageService:**
- `SendAsync(chatId, content, replyToMessageId?)` → MessageAckPacket
- `OnMessageReceived` — событие для входящих сообщений

**ChatService:**
- `CreateAsync(title, chatType, participantIds)` → ChatCreateResponsePacket
- `GetChatsAsync(userId)` → List\<ChatDto\>
- `GetHistoryAsync(chatId, limit)` → List\<MessageDto\>

---

## Схема БД (SQL Server)

```sql
users
├── id (bigint, IDENTITY, PK)
├── username (nvarchar(50))         -- добавлено в коде, нет в SQL скрипте
├── first_name (nvarchar(50))
├── last_name (nvarchar(50))
├── email (nvarchar(50), nullable)
├── numberphone (nvarchar(30))
├── password_hash (nvarchar)
├── avatar_url (nvarchar, nullable)
├── crated_at (datetime2)
└── last_seen_at (datetime2)

chats
├── id (bigint, IDENTITY, PK)
├── title (nvarchar(100))
├── chat_type (nvarchar(20), CHECK: direct/group/channel)
└── created_at (datetime2)

chat_members
├── chat_id (bigint, FK → chats)
├── user_id (bigint, FK → users)
├── connetion_at (datetime2)
└── PK: (user_id, chat_id)

messages
├── id (bigint, IDENTITY, PK)
├── chat_id (bigint, FK → chats)
├── sender_id (bigint, FK → users)
├── content (nvarchar(max))
├── edited (bit)
├── reply_to_message_id (bigint, nullable)
├── send_at (datetime2)
└── status (nvarchar(20), CHECK: sent_except/delivered/is_read)
```

**Известные проблемы SQL скрипта:**
- `username` отсутствует в `scripts/script.sql` (только в EF коде)
- `DROP TABLE` идут после `CREATE TABLE` в `scripts/script.sql`
- Опечатки: `crated_at` (→ created_at), `connetion_at` (→ connection_at), `sent_except` (→ sent)

---

## Конфигурация

### Server/appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MessengerSlayer;..."
  },
  "TcpServer": {
    "Port": 5000,
    "CertificatePath": "Certs/server.pfx",
    "CertificatePassword": ""
  }
}
```

### Client/appsettings.json
```json
{
  "Server": {
    "Host": "localhost",
    "Port": 5000
  }
}
```

---

## Code Style (соблюдён)

- `sealed class` для всех классов
- `{ get; init; }` для DTO, `{ get; set; }` для Entities
- PascalCase для классов/методов/свойств
- camelCase для локальных переменных
- `Async` суффикс для async методов
- CancellationToken как последний параметр
- Allman brace style (отступ перед `{`)
- 4 пробела отступ
- Nullable enabled
