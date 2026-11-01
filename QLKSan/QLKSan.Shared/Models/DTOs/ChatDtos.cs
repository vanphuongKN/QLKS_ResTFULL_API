using System;
using System.Collections.Generic;

namespace QLKSan.Models.DTOs
{
    // Hội thoại với trợ lý ảo (QLKSan.ChatBot): POST /api/chat/conversations, GET /api/chat/conversations/{id}
    public class ChatConversationDto
    {
        public Guid MaHoiThoai { get; set; }
        public DateTime ThoiGianTao { get; set; }
        public List<ChatMessageDto> TinNhan { get; set; } = new List<ChatMessageDto>();
    }

    public static class ChatRoles
    {
        public const string Khach = "khach";
        public const string Bot = "bot";
    }

    // Một tin nhắn; tin của bot có thể kèm liên kết (đặt phòng điền sẵn, xem hạng phòng) và câu hỏi gợi ý
    public class ChatMessageDto
    {
        public string VaiTro { get; set; }
        public string NoiDung { get; set; }
        public DateTime ThoiGian { get; set; }
        public List<ChatLinkDto> LienKet { get; set; } = new List<ChatLinkDto>();
        public List<string> GoiY { get; set; } = new List<string>();

        // Provider đã trả lời (RuleBased, OpenAI, ...); "(dự phòng)" khi provider chính lỗi
        public string Provider { get; set; }
    }

    public class ChatLinkDto
    {
        public string Nhan { get; set; }
        public string Url { get; set; }
    }
}
