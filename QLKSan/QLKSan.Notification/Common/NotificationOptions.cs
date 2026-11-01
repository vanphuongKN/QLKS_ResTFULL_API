namespace QLKSan.Notification.Common
{
    // Ánh xạ mục "RabbitMq" trong appsettings.json
    public class RabbitMqOptions
    {
        public const string SectionName = "RabbitMq";
        public string Uri { get; set; } = "amqp://guest:guest@localhost:5672/";
    }

    // Ánh xạ mục "Email": để trống Smtp:Host thì email được ghi thành file .eml trong ThuMucLuu (môi trường phát triển / demo)
    public class EmailOptions
    {
        public const string SectionName = "Email";

        public string TenKhachSan { get; set; } = "LUXE RESERVE";
        public string NguoiGui { get; set; } = "datphong@luxereserve.demo";
        public string Hotline { get; set; }
        public string ThuMucLuu { get; set; } = "mail";
        public SmtpOptions Smtp { get; set; } = new SmtpOptions();
    }

    public class SmtpOptions
    {
        public string Host { get; set; }
        public int Port { get; set; } = 587;
        public string TenDangNhap { get; set; }
        public string MatKhau { get; set; }
        public bool DungSsl { get; set; } = true;
    }
}
