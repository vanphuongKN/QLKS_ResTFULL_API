using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using QLKSan.Models.Events;
using QLKSan.Notification.Business;
using QLKSan.Notification.Common;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace QLKSan.Notification.Messaging
{
    // Tình trạng hiển thị ở /health
    public class ConsumerStatus
    {
        public bool DaKetNoi { get; set; }
        public int DaGui { get; set; }
        public DateTime? LanGuiCuoi { get; set; }
    }

    // Nhận sự kiện phiếu đặt phòng từ hàng đợi thông báo và gửi email cho khách.
    // Mỗi sự kiện chỉ xử lý một lần (đánh dấu theo MessageId), lỗi gửi thì trả lại hàng đợi để thử lại.
    public class BookingEventConsumer : BackgroundService
    {
        private readonly RabbitMqOptions _rabbit;
        private readonly EmailOptions _email;
        private readonly IEmailSender _sender;
        private readonly ConsumerStatus _status;
        private readonly ILogger<BookingEventConsumer> _logger;
        private readonly string _thuMucDaXuLy;

        public BookingEventConsumer(IOptions<RabbitMqOptions> rabbit, IOptions<EmailOptions> email, IEmailSender sender, ConsumerStatus status,
            IHostEnvironment environment, ILogger<BookingEventConsumer> logger)
        {
            _rabbit = rabbit.Value;
            _email = email.Value;
            _sender = sender;
            _status = status;
            _logger = logger;
            _thuMucDaXuLy = Path.GetFullPath(Path.Combine(environment.ContentRootPath, _email.ThuMucLuu, "da-xu-ly"));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Directory.CreateDirectory(_thuMucDaXuLy);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ConsumeAsync(stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _status.DaKetNoi = false;
                    _logger.LogWarning(ex, "Mất kết nối RabbitMQ, thử lại sau 5 giây");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }

        private async Task ConsumeAsync(CancellationToken ct)
        {
            var factory = new ConnectionFactory { Uri = new Uri(_rabbit.Uri), ClientProvidedName = "QLKSan.Notification" };
            await using var connection = await factory.CreateConnectionAsync(ct);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
            await channel.ExchangeDeclareAsync(MessagingNames.Exchange, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: ct);
            await channel.QueueDeclareAsync(MessagingNames.HangDoiThongBao, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
            await channel.QueueBindAsync(MessagingNames.HangDoiThongBao, MessagingNames.Exchange, MessagingNames.KhoaDatPhong, cancellationToken: ct);
            await channel.BasicQosAsync(0, 10, false, ct);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    await HandleAsync(ea, ct);
                    await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Không xử lý được sự kiện {MessageId}, trả lại hàng đợi", ea.BasicProperties.MessageId);
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                    await channel.BasicNackAsync(ea.DeliveryTag, false, true, ct);
                }
            };
            await channel.BasicConsumeAsync(MessagingNames.HangDoiThongBao, autoAck: false, consumer: consumer, cancellationToken: ct);
            _status.DaKetNoi = true;
            _logger.LogInformation("Đang nhận sự kiện từ hàng đợi {Queue}", MessagingNames.HangDoiThongBao);

            // Giữ kết nối tới khi dừng service hoặc kết nối bị đóng (vòng ngoài sẽ kết nối lại)
            while (!ct.IsCancellationRequested && connection.IsOpen && channel.IsOpen)
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
            _status.DaKetNoi = false;
            if (!ct.IsCancellationRequested) throw new InvalidOperationException("Kết nối RabbitMQ đã đóng.");
        }

        private async Task HandleAsync(BasicDeliverEventArgs ea, CancellationToken ct)
        {
            var su = JsonSerializer.Deserialize<BookingEvent>(Encoding.UTF8.GetString(ea.Body.Span));
            if (su == null) return;
            var maSuKien = su.MaSuKien != Guid.Empty ? su.MaSuKien.ToString() : ea.BasicProperties.MessageId;

            // Sự kiện có thể được gửi lại (outbox gửi lại sau sự cố): đã xử lý thì bỏ qua
            var daXuLy = Path.Combine(_thuMucDaXuLy, maSuKien);
            if (File.Exists(daXuLy)) return;

            var email = EmailComposer.Compose(su, _email);
            if (email != null)
            {
                await _sender.SendAsync(email, ct);
                _status.DaGui++;
                _status.LanGuiCuoi = DateTime.Now;
                _logger.LogInformation("Đã gửi email {Loai} phiếu {MaDatPhong} tới {Email}", su.Loai, su.MaDatPhong, email.NguoiNhan);
            }
            await File.WriteAllTextAsync(daXuLy, $"{su.Loai} {su.MaDatPhong} {DateTime.Now:O} {(email == null ? "không có email" : email.NguoiNhan)}", ct);
        }
    }
}
