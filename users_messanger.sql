Create Table users(
    id bigint primary key,
    first_name nvarchar(50) NOT NULL,
    last_name  nvarchar(50) NOT NULL,
    email nvarchar(50), 
    numberphone nvarchar(30) NOT NULL,
    password_hash  nvarchar NOT NULL,
    avatar_url NVARCHAR, 
    crated_at DATETIME2(0) DEFAULT GETDATE(),
    last_seen_at DATETIME2(0) DEFAULT  SYSUTCDATETIME()
)


select * from users

Create Table chats(

)