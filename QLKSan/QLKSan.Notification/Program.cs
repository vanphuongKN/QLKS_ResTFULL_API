using QLKSan.Notification.Business;
using QLKSan.Notification.Common;
using QLKSan.Notification.Messaging;

namespace QLKSan.Notification
{
    // Service thông báo: nhận sự kiện phiếu đặt phòng (đặt mới, huỷ, nhận phòng) từ RabbitMQ và gửi email cho khách
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
            builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
            builder.Services.AddSingleton<ConsumerStatus>();
            builder.Services.AddSingleton<IEmailSender, EmailSender>();
            builder.Services.AddHostedService<BookingEventConsumer>();

            var app = builder.Build();

            // Còn kết nối RabbitMQ không, đã gửi bao nhiêu email từ lúc khởi động
            app.MapGet("/health", (ConsumerStatus s) => s.DaKetNoi
                ? Results.Ok(new { status = "Healthy", service = "QLKSan.Notification", s.DaGui, s.LanGuiCuoi })
                : Results.Json(new { status = "Unhealthy", service = "QLKSan.Notification", rabbitMq = "Disconnected" }, statusCode: StatusCodes.Status503ServiceUnavailable));

            app.Run();
        }
    }
}
