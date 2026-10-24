using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QLKSan.Common;
using QLKSan.Data;
using QLKSan.Enums;

namespace QLKSan.Business
{
    // Tác vụ nền: định kỳ huỷ các phiếu Chờ duyệt/Đã duyệt đã qua ngày nhận phòng mà khách không đến (no-show)
    public class NoShowCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly HotelPolicy _policy;
        private readonly ILogger<NoShowCleanupService> _logger;

        public NoShowCleanupService(IServiceScopeFactory scopeFactory, IOptions<HotelPolicy> policy, ILogger<NoShowCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _policy = policy.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_policy.TuDongHuyKhachKhongDen) return;

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(1, _policy.ChuKyKiemTraKhongDenPhut)));
            do
            {
                try
                {
                    await CancelNoShowBookingsAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Không để lỗi của tác vụ nền làm dừng cả API
                    _logger.LogError(ex, "Lỗi khi tự động huỷ phiếu khách không đến");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        private async Task CancelNoShowBookingsAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var today = DateTime.Today;

            var overdue = await context.PhieuDatPhongs
                .Where(p => (p.TrangThai == BookingStatus.Pending || p.TrangThai == BookingStatus.Approved)
                         && p.NgayCheckInDuKien < today)
                .ToListAsync(cancellationToken);
            if (overdue.Count == 0) return;

            foreach (var booking in overdue)
            {
                booking.TrangThai = BookingStatus.Cancelled;
                booking.GhiChu = BookingNotes.Append(booking.GhiChu, $"[Tự động huỷ {DateTime.Now:dd/MM/yyyy HH:mm}] Khách không đến nhận phòng");
            }
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Đã tự động huỷ {Count} phiếu khách không đến: {Ids}",
                overdue.Count, string.Join(", ", overdue.Select(b => b.MaDatPhong)));
        }
    }

    public static class BookingNotes
    {
        public const int MaxLength = 1000;

        // Nối thêm ghi chú vào phiếu, giữ phần mới nhất nếu vượt độ dài cột GhiChu
        public static string Append(string current, string note)
        {
            var combined = string.IsNullOrWhiteSpace(current) ? note : $"{current} | {note}";
            return combined.Length <= MaxLength ? combined : combined.Substring(combined.Length - MaxLength);
        }
    }
}
