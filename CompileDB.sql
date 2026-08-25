Create Table users(
    id bigint IDENTITY(1,1) primary key,
    first_name nvarchar(50) NOT NULL,
    last_name  nvarchar(50) NOT NULL,
    email nvarchar(50), 
    numberphone nvarchar(30) NOT NULL,
    password_hash  nvarchar NOT NULL,
    avatar_url NVARCHAR, 
    crated_at DATETIME2(0) DEFAULT GETDATE(),
    last_seen_at DATETIME2(0) DEFAULT  SYSUTCDATETIME()
);

DROP TABLE  IF EXISTS chat_members;
DROP TABLE IF EXISTS users;
DROP TABLE IF EXISTS  chats;

select * from chats
-- не создано

Create Table chats (
    id bigint IDENTITY(1,1) primary key,  
    title NVARCHAR(100) NOT NULL,
    chat_type NVARCHAR(20) NOT NULL DEFAULT 'direct'
        CHECK(chat_type IN('direct','group','channel')), -- ограничеваем ввод типов на выделенный список
    created_at DATETIME2(0) DEFAULT GETDATE()
);

Create Table chat_members( -- промежуточная таблица 
    chat_id bigint NOT NULL,
    user_id bigint NOT NULL,

    connetion_at DATETIME2(0) DEFAULT GETDATE(),
    
    CONSTRAINT PK_chat_members PRIMARY KEY (user_id , chat_id), -- составной ключ 

    -- внешние ключи 
    CONSTRAINT FK_chat_members_users FOREIGN KEY (user_id)
        REFERENCES users(id) ON DELETE CASCADE , -- проверяет существование пользователя а при удаления подчищает все связи 

    CONSTRAINT FK_chat_members_chats FOREIGN KEY (chat_id)
        REFERENCES chats(id) ON DELETE CASCADE

);

Create Table messages (
    id bigint IDENTITY(1,1),
    chat_id bigint NOT NULL,
    sender_id bigint NOT NULL, 
    content NVARCHAR(MAX) NOT NULL,
    edited BIT, 
    reply_to_message_id bigint,
    send_at DATETIME2(0) DEFAULT GETDATE(),

    -- тут используется связь один ко многим

    CONSTRAINT PK_messages PRIMARY KEY (id),

    CONSTRAINT FK_chats FOREIGN KEY (chat_id)
        REFERENCES chats(id) ON DELETE CASCADE,
    
    CONSTRAINT FK_users FOREIGN KEY (sender_id)
        REFERENCES users(id) ON DELETE CASCADE
);

alter table messages 
add status NVARCHAR(20) DEFAULT 'sent_except'
    CHECK(status IN ('sent_except','delivered','is_read'));

SELECT * FROM users;
SELECT * FROM chats;
SELECT * FROM chat_members;
SELECT * FROM messages;

