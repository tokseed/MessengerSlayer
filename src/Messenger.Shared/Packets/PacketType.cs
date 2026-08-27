namespace Messenger.Shared.Packets;

public enum PacketType
{
    Auth,
    AuthResponse,
    Register,
    RegisterResponse,
    ProfileRequest,
    ProfileResponse,
    ProfileUpdate,
    ProfileUpdateResponse,
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
