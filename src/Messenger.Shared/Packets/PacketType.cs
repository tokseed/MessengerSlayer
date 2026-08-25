namespace Messenger.Shared.Packets;

public enum PacketType
{
    Auth,
    AuthResponse,
    Register,
    RegisterResponse,
    Message,
    MessageAck,
    ChatCreate,
    ChatCreateResponse,
    ChatListRequest,
    ChatListResponse,
    UserListRequest,
    UserListResponse,
    MessageHistoryRequest,
    MessageHistoryResponse,
    Disconnect
}
