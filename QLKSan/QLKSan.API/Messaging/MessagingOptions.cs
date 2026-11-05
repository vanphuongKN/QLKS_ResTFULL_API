namespace QLKSan.Messaging
{
    // Ánh xạ mục "Messaging" trong appsettings.json
    public class MessagingOptions
    {
        public const string SectionName = "Messaging";

        // Tắt (mặc định khi phát triển): không ghi sự kiện, không cần RabbitMQ
        public bool Enabled { get; set; }
        public string RabbitMqUri { get; set; } = "amqp://guest:guest@localhost:5672/";

        // Chu kỳ tác vụ nền quét outbox
        public int ChuKyGuiGiay { get; set; } = 5;
    }
}
