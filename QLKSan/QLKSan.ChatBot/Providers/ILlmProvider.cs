using QLKSan.Models.DTOs;

namespace QLKSan.ChatBot.Providers
{
    // Một lượt trong lịch sử hội thoại (VaiTro: ChatRoles.Khach | ChatRoles.Bot)
    public record ChatTurn(string VaiTro, string NoiDung);

    public class BotReply
    {
        public string NoiDung { get; set; }
        public List<ChatLinkDto> LienKet { get; set; } = new List<ChatLinkDto>();
        public List<string> GoiY { get; set; } = new List<string>();
    }

    // Nguồn trả lời của trợ lý ảo; chọn bằng cấu hình ChatBot:Provider (RuleBased | OpenAI; thêm Claude / Gemini bằng một lớp mới)
    public interface ILlmProvider
    {
        string Ten { get; }

        // lichSu: các lượt trước câu hỏi hiện tại (cũ trước, mới sau)
        Task<BotReply> ReplyAsync(IReadOnlyList<ChatTurn> lichSu, string cauHoi, CancellationToken ct);
    }
}
