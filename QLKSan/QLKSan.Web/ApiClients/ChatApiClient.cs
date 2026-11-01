using QLKSan.Infrastructure;
using QLKSan.Models.DTOs;
using QLKSan.Models.Requests;

namespace QLKSan.ApiClients
{
    // ===== /api/chat/conversations: trợ lý ảo (QLKSan.ChatBot, qua Gateway) =====
    public interface IChatApiClient
    {
        Task<ChatConversationDto> CreateAsync();

        // null = hội thoại không còn (vd: CSDL của ChatBot đã xoá)
        Task<ChatConversationDto> GetAsync(Guid maHoiThoai);
        Task<ChatMessageDto> SendAsync(Guid maHoiThoai, string noiDung);
    }

    public class ChatApiClient : ApiClientBase, IChatApiClient
    {
        public ChatApiClient(HttpClient http) : base(http) { }

        public Task<ChatConversationDto> CreateAsync() => HttpPostAsync<ChatConversationDto>("api/chat/conversations");

        public Task<ChatConversationDto> GetAsync(Guid maHoiThoai) => HttpGetOrDefaultAsync<ChatConversationDto>($"api/chat/conversations/{maHoiThoai}");

        public Task<ChatMessageDto> SendAsync(Guid maHoiThoai, string noiDung)
            => HttpPostAsync<ChatMessageDto>($"api/chat/conversations/{maHoiThoai}/messages", new SendChatMessageRequest { NoiDung = noiDung });
    }
}
