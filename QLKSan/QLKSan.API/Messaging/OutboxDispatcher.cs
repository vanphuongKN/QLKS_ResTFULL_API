using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QLKSan.Data;
using QLKSan.Models.Events;
using RabbitMQ.Client;

namespace QLKSan.Messaging
{
    // Tác vụ nền: đẩy sự kiện chưa gửi trong outbox lên RabbitMQ (có xác nhận của broker), lần lượt theo thời gian tạo
    public class OutboxDispatcher : BackgroundService
    {
        private const int SoSuKienMoiLan = 50;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly MessagingOptions _options;
        private readonly ILogger<OutboxDispatcher> _logger;
        private IConnection _connection;
        private IChannel _channel;

        public OutboxDispatcher(IServiceScopeFactory scopeFactory, IOptions<MessagingOptions> options, ILogger<OutboxDispatcher> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1, _options.ChuKyGuiGiay)));
            do
            {
                try
                {
                    await DispatchAsync(stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    // Broker chưa sẵn sàng / mất kết nối: thử lại ở chu kỳ sau, sự kiện vẫn nằm trong outbox
                    _logger.LogWarning(ex, "Chưa gửi được sự kiện lên RabbitMQ, sẽ thử lại");
                    await CloseAsync();
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        private async Task DispatchAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var cho = await db.SuKienChos.Where(s => s.ThoiGianGui == null)
                .OrderBy(s => s.ThoiGianTao).Take(SoSuKienMoiLan).ToListAsync(ct);
            if (cho.Count == 0) return;

            await EnsureChannelAsync(ct);
            foreach (var su in cho)
            {
                try
                {
                    var props = new BasicProperties { Persistent = true, MessageId = su.MaSuKien.ToString(), ContentType = "application/json", Type = su.Loai };
                    await _channel.BasicPublishAsync(MessagingNames.Exchange, su.Loai, false, props, Encoding.UTF8.GetBytes(su.NoiDung), ct);
                    su.ThoiGianGui = DateTime.Now;
                }
                catch (Exception ex)
                {
                    su.SoLanThu++;
                    su.LoiCuoi = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
                    await db.SaveChangesAsync(CancellationToken.None);
                    throw;
                }
                // Lưu từng sự kiện: dừng giữa chừng không gửi lại cả lô (service nhận vẫn bỏ qua sự kiện trùng)
                await db.SaveChangesAsync(CancellationToken.None);
            }
        }

        private async Task EnsureChannelAsync(CancellationToken ct)
        {
            if (_channel is { IsOpen: true }) return;
            await CloseAsync();
            var factory = new ConnectionFactory { Uri = new Uri(_options.RabbitMqUri), ClientProvidedName = "QLKSan.API" };
            _connection = await factory.CreateConnectionAsync(ct);
            _channel = await _connection.CreateChannelAsync(new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true), ct);
            await _channel.ExchangeDeclareAsync(MessagingNames.Exchange, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: ct);
            // Khai báo sẵn hàng đợi thông báo: sự kiện phát trước khi service thông báo chạy lần đầu không bị mất
            await _channel.QueueDeclareAsync(MessagingNames.HangDoiThongBao, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
            await _channel.QueueBindAsync(MessagingNames.HangDoiThongBao, MessagingNames.Exchange, MessagingNames.KhoaDatPhong, cancellationToken: ct);
        }

        private async Task CloseAsync()
        {
            try
            {
                if (_channel != null) await _channel.DisposeAsync();
                if (_connection != null) await _connection.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Lỗi khi đóng kết nối RabbitMQ");
            }
            _channel = null;
            _connection = null;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);
            await CloseAsync();
        }
    }
}
