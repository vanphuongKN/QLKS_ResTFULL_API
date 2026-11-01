namespace QLKSan.ChatBot.Common
{
    // Ánh xạ mục "ChatBot" trong appsettings.json
    public class ChatBotOptions
    {
        public const string SectionName = "ChatBot";

        // Provider trả lời: RuleBased (không cần khoá, mặc định) | OpenAI. Provider chính lỗi thì tự dùng RuleBased.
        public string Provider { get; set; } = "RuleBased";

        // Địa chỉ Gateway để gọi các endpoint công khai của API lõi
        public string ApiBaseUrl { get; set; } = "https://localhost:7000/";

        // Địa chỉ trang Web dùng cho liên kết đặt phòng / xem hạng phòng gửi cho khách
        public string WebBaseUrl { get; set; } = "https://localhost:7234/";

        // Số tin nhắn gần nhất gửi kèm làm ngữ cảnh cho mô hình
        public int SoTinNhanNguCanh { get; set; } = 12;

        public OpenAiOptions OpenAI { get; set; } = new OpenAiOptions();
    }

    public class OpenAiOptions
    {
        // Chỉ đặt bằng biến môi trường ChatBot__OpenAI__ApiKey hoặc user-secrets, không ghi vào appsettings.json
        public string ApiKey { get; set; }
        public string Model { get; set; } = "gpt-4o-mini";
        public string BaseUrl { get; set; } = "https://api.openai.com/v1/";
        public int TimeoutGiay { get; set; } = 30;
    }
}
